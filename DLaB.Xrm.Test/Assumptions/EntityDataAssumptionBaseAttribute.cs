﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using DLaB.Common;
using DLaB.Xrm.Client;
using Microsoft.Xrm.Sdk;


namespace DLaB.Xrm.Test.Assumptions
{
    [AttributeUsage(AttributeTargets.Class)]
    public abstract class EntityDataAssumptionBaseAttribute : Attribute
    {
        protected readonly string EntityXmlDirectoryRelativePath = Path.Combine("Assumptions", "Entity Xml");

        private IEnumerable<Type> _prerequisites;
        private IEnumerable<Type> Prerequisites
        {
            get
            {
                return _prerequisites ??
                    (_prerequisites = (GetType().GetCustomAttributes(false).Select(a => a as PrerequisiteAssumptionsAttribute).FirstOrDefault()
                                      ??
                                      new PrerequisiteAssumptionsAttribute(new Type[0])
                                      ).Prerequisites);
            }
        }

        private AssumedEntities _assumedEntities;
        private HashSet<Type> _currentlyProcessingPreReqs;

        /// <summary>
        /// Checks to ensure that the assumption being asked for has been defined in the PrerequisiteAssumptions for the current Assumption
        /// </summary>
        /// <typeparam name="TAttribute"></typeparam>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        protected TEntity GetAssumedEntity<TAttribute, TEntity>()
            where TAttribute : EntityDataAssumptionBaseAttribute
            where TEntity : Entity
        {
            if (!Prerequisites.Any())
            {
                throw new Exception(String.Format(
                    "Assumption {0} is attempting to retrieve Assumption of type {1}, but it has not defined a PrerequisiteAssumptions Attribute.{2}{2}" +
                    "Please add a PrerequisiteAssumption Attribute with a(n) {1} in it's constructor to Assumption {0}.",
                    ShortName, GetShortName(typeof(TAttribute)), Environment.NewLine));
            }

            if (Prerequisites.All(t => t != typeof(TAttribute)))
            {
                throw new Exception(String.Format(
                    "Assumption {0} is attempting to retrieve Assumption of type {1}, but it has not defined it in it's PrerequisiteAssumptions Attribute.{2}{2}" +
                    "Please add a(n) {1} to the PrerequisiteAssumption Attribute in Assumption {0}.",
                    ShortName, GetShortName(typeof(TAttribute)), Environment.NewLine));
            }

            return _assumedEntities.Get<TAttribute, TEntity>();
        }

        /// <summary>
        /// Gets the name of the type, without the "Attribute" postfix
        /// </summary>
        private String ShortName
        {
            get { return GetShortName(GetType()); }
        }

        /// <summary>
        /// Gets the name of the type, without the "Attribute" postfix
        /// </summary>
        private static string GetShortName(Type type)
        {
            var name = type.Name;
            if (name.EndsWith("Attribute", StringComparison.InvariantCultureIgnoreCase))
            {
                name = name.Substring(0, name.Length - "Attribute".Length);
            }
            return name;
        }

        /// <summary>
        /// Return the entity assumed to exist or null if not found.
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        protected abstract Entity RetrieveEntity(IOrganizationService service);

        /// <summary>
        /// Adds the entities assumed to exist to the AssumedEntities Collection
        /// </summary>
        /// <param name="service"></param>
        /// <param name="assumedEntities">Collection of Assumptions that have already been verified to be true</param>
        /// <returns></returns>
        public void AddAssumedEntities(IOrganizationService service, AssumedEntities assumedEntities)
        {
            AddAssumedEntitiesWithPreReqInfiniteLoopPrevention(service, assumedEntities, new HashSet<Type>());
        }

        protected void AddAssumedEntitiesWithPreReqInfiniteLoopPrevention(IOrganizationService service, AssumedEntities assumedEntities,
                                                                          HashSet<Type> currentlyProcessingPreReqs)
        {
            _assumedEntities = assumedEntities;
            _currentlyProcessingPreReqs = currentlyProcessingPreReqs;
            var type = GetType();

            if (_assumedEntities.Contains(this))
            {
                return;
            }
            if (_currentlyProcessingPreReqs.Contains(type))
            {
                ThrowErrorPreventingInfiniteLoop(type);
            }
            _currentlyProcessingPreReqs.Add(type);
            AddPrerequisiteAssumptions(service);
            AddAssumedEntitiesInternal(service);
            _currentlyProcessingPreReqs.Remove(type);
        }

        private void ThrowErrorPreventingInfiniteLoop(Type type)
        {
            if (_currentlyProcessingPreReqs.Count == 1)
            {
                throw new Exception(String.Format("Prerequisite Assumption Loop!  {0} called itself!", type));
            }
            var sb = new StringBuilder();
            sb.Append(_currentlyProcessingPreReqs.First().Name + " called " + _currentlyProcessingPreReqs.Skip(1).First().Name);
            foreach (var prereq in _currentlyProcessingPreReqs.Skip(2))
            {
                sb.Append(" which called " + prereq.Name);
            }
            sb.Append(" which attempt to call " + type.Name);
            throw new Exception("Prerequisite Assumption Loop!  " + sb);
        }

        protected virtual void AddAssumedEntitiesInternal(IOrganizationService service)
        {
            var entity = RetrieveEntity(service);
            entity = VerifyAssumption(service, entity);
            _assumedEntities.Add(this, entity);
        }

        protected void AddAssumedEntity(IOrganizationService service, EntityDataAssumptionBaseAttribute assumption)
        {
            assumption.AddAssumedEntitiesWithPreReqInfiniteLoopPrevention(service, _assumedEntities, _currentlyProcessingPreReqs);
        }

        private void AddPrerequisiteAssumptions(IOrganizationService service)
        {
            foreach (var assumption in Prerequisites.Select(prereq => (EntityDataAssumptionBaseAttribute)Activator.CreateInstance(prereq))
                                                    .Where(a => !_assumedEntities.Contains(a)))
            {

                assumption.AddAssumedEntitiesWithPreReqInfiniteLoopPrevention(service, _assumedEntities, _currentlyProcessingPreReqs);
            }
        }


        /// <summary>
        /// Throws an error if Entity is null and it's using a real CRM database
        /// -or-
        /// Throws an error if the CRM database is a local CRM database, and there are no serialized version of the files
        /// to deserialize
        /// </summary>
        /// <param name="service"></param>
        /// <param name="entity"></param>
        private Entity VerifyAssumption(IOrganizationService service, Entity entity)
        {
            if (entity == null)
            {
                var mock = service as FakeIOrganizationService;
                // If the service is a Mock, get the Actual Service to determine if it is local or not...
                if (
                    (mock != null && mock.ActualService as LocalCrm.LocalCrmDatabaseOrganizationService == null) ||
                    (mock == null && service as LocalCrm.LocalCrmDatabaseOrganizationService == null) ||
                    FileIsNullOrEmpty(GetSerializedFilePath()))
                {
                    throw new Exception(String.Format("Assumption {0} made an ass out of you and me.  The entity assumed to be there, was not found.", ShortName));
                }

                LocalCrm.LocalCrmDatabaseOrganizationService localService;
                if (mock == null)
                {
                    localService = (LocalCrm.LocalCrmDatabaseOrganizationService) service;
                }
                else
                {
                    localService = (LocalCrm.LocalCrmDatabaseOrganizationService) mock.ActualService;
                }

                entity = TestBase.GetTestXml(ShortName, EntityXmlDirectoryRelativePath, GetType()).DeserializeEntity();
                var isSelfReferencing = CreateForeignReferences(localService, entity);
                if (isSelfReferencing)
                {
                    service.Update(entity);
                }
                else
                {
                    entity.Id = service.Create(entity);
                }
            }
            else if (Debugger.IsAttached)
            {
                TfsHelper.CheckoutAndUpdateFileIfDifferent(GetSerializedFilePath(), entity.Serialize(true));
            }

            return entity;
        }

        /// <summary>
        /// Creates all foreign references that don't exist.  This is usally due to the serialization grabbing more values than actually needed.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="entity"></param>
        private bool CreateForeignReferences(LocalCrm.LocalCrmDatabaseOrganizationService service, Entity entity)
        {
            var isSelfReferencing = false;
            var toRemove = new List<string>();
            foreach (var attribute in entity.Attributes)
            {
                var foreign = attribute.Value as EntityReference;
                if (foreign == null)
                {
                    continue;
                }

                // Check to makes sure the type has been defined.  Don't create the Foreign Reference, and remove the attribute from the collection.
                if (!service.Info.IsTypeDefined(foreign.LogicalName))
                {
                    toRemove.Add(attribute.Key);
                    continue;
                }

                if (foreign.Id == entity.Id)
                {
                    isSelfReferencing = true;
                }

                if (service.GetEntitiesById(foreign.LogicalName, foreign.Id).Count == 0)
                {
                    service.Create(new Entity { Id = foreign.Id, LogicalName = foreign.LogicalName });
                }
            }

            foreach (var key in toRemove)
            {
                entity.Attributes.Remove(key);
            }

            return isSelfReferencing;
        }

        private string GetSerializedFilePath()
        {
            return Path.Combine(TestBase.GetProjectPath(GetType()), EntityXmlDirectoryRelativePath, ShortName + ".xml");
        }

        private static bool FileIsNullOrEmpty(string filePath)
        {
            return !(File.Exists(filePath) && new FileInfo(filePath).Length > 0);
        }

        // Has to be protected inner class since only EntityDataAssumptionBase types should use it.
        /// <summary>
        /// Lists the EntityDataAssumptions that are required by the decorated class.  These assumptions will be loaded automatically by the EntityDataAssumptionBase.
        /// </summary>
        [AttributeUsage(AttributeTargets.Class, Inherited = true)]
        protected class PrerequisiteAssumptionsAttribute : Attribute
        {
            public IEnumerable<Type> Prerequisites { get; set; }

            // ReSharper disable once UnusedMember.Local
            private PrerequisiteAssumptionsAttribute() { }

            public PrerequisiteAssumptionsAttribute(params Type[] prerequisites)
            {
                Prerequisites = prerequisites;
            }
        }

        public class AssumptionEntityPair
        {
            public Type Assumption { get; set; }
            public Entity Entity { get; set; }

            public AssumptionEntityPair(Type assumption, Entity entity)
            {
                Assumption = assumption;
                Entity = entity;
            }
        }
    }
}