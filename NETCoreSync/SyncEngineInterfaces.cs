﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Linq.Expressions;
using System.Linq.Dynamic.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NETCoreSync.Exceptions;
using System.Reflection;
using System.IO;
using System.IO.Compression;

namespace NETCoreSync
{
    public abstract partial class SyncEngine
    {
        public virtual bool IsServerEngine()
        {
            //must implement if SyncConfiguration.TimeStampStrategy = UseGlobalTimeStamp
            throw new NotImplementedException();
        }

        public virtual long GetClientLastSync()
        {
            //must implement if SyncConfiguration.TimeStampStrategy = UseGlobalTimeStamp
            throw new NotImplementedException();
        }

        public virtual void SetClientLastSync(long lastSync)
        {
            //must implement if SyncConfiguration.TimeStampStrategy = UseGlobalTimeStamp
            throw new NotImplementedException();
        }

        public virtual long GetNextTimeStamp()
        {
            //must implement if SyncConfiguration.TimeStampStrategy = UseEachDatabaseInstanceTimeStamp
            throw new NotImplementedException();
        }

        public virtual List<KnowledgeInfo> GetAllKnowledgeInfos(string synchronizationId, Dictionary<string, object> customInfo)
        {
            //must implement if SyncConfiguration.TimeStampStrategy = UseEachDatabaseInstanceTimeStamp
            throw new NotImplementedException();
        }

        public virtual void CreateOrUpdateKnowledgeInfo(KnowledgeInfo knowledgeInfo, string synchronizationId, Dictionary<string, object> customInfo)
        {
            //must implement if SyncConfiguration.TimeStampStrategy = UseEachDatabaseInstanceTimeStamp
            throw new NotImplementedException();
        }

        public virtual object StartTransaction(Type classType, OperationType operationType, string synchronizationId, Dictionary<string, object> customInfo)
        {
            return null;
        }

        public virtual void CommitTransaction(Type classType, object transaction, OperationType operationType, string synchronizationId, Dictionary<string, object> customInfo)
        {
        }

        public virtual void RollbackTransaction(Type classType, object transaction, OperationType operationType, string synchronizationId, Dictionary<string, object> customInfo)
        {
        }

        public virtual void EndTransaction(Type classType, object transaction, OperationType operationType, string synchronizationId, Dictionary<string, object> customInfo)
        {
        }

        public abstract IQueryable GetQueryable(Type classType, object transaction, OperationType operationType, string synchronizationId, Dictionary<string, object> customInfo);

        public abstract string SerializeDataToJson(Type classType, object data, object transaction, OperationType operationType, string synchronizationId, Dictionary<string, object> customInfo);

        public abstract object DeserializeJsonToNewData(Type classType, JObject jObject, object transaction, OperationType operationType, string synchronizationId, Dictionary<string, object> customInfo);

        public abstract object DeserializeJsonToExistingData(Type classType, JObject jObject, object data, object transaction, OperationType operationType, string synchronizationId, Dictionary<string, object> customInfo);

        public abstract void PersistData(Type classType, object data, bool isNew, object transaction, OperationType operationType, string synchronizationId, Dictionary<string, object> customInfo);

        public virtual object TransformIdType(Type classType, JValue id, object transaction, OperationType operationType, string synchronizationId, Dictionary<string, object> customInfo)
        {
            return id;
        }

        public virtual void PostEventDelete(Type classType, object id, string synchronizationId, Dictionary<string, object> customInfo)
        {
        }

        public void HookPreInsertOrUpdate(object data)
        {
            if (data == null) throw new NullReferenceException(nameof(data));
            SyncConfiguration.SchemaInfo schemaInfo = GetSchemaInfo(SyncConfiguration, data.GetType());
            if (SyncConfiguration.TimeStampStrategy == SyncConfiguration.TimeStampStrategyEnum.GlobalTimeStamp)
            {
                long nowTicks = GetNowTicks();
                if (!IsServerEngine())
                {
                    long lastSync = GetClientLastSync();
                    if (nowTicks <= lastSync) throw new SyncEngineConstraintException("System Date and Time is older than the lastSync value");
                }
                data.GetType().GetProperty(schemaInfo.PropertyInfoLastUpdated.Name).SetValue(data, nowTicks);
            }
            if (SyncConfiguration.TimeStampStrategy == SyncConfiguration.TimeStampStrategyEnum.DatabaseTimeStamp)
            {
                long timeStamp = GetNextTimeStamp();
                data.GetType().GetProperty(schemaInfo.PropertyInfoDatabaseInstanceId.Name).SetValue(data, null);
                data.GetType().GetProperty(schemaInfo.PropertyInfoLastUpdated.Name).SetValue(data, timeStamp);
            }
        }

        public void HookPreDelete(object data)
        {
            if (data == null) throw new NullReferenceException(nameof(data));
            SyncConfiguration.SchemaInfo schemaInfo = GetSchemaInfo(SyncConfiguration, data.GetType());
            if (SyncConfiguration.TimeStampStrategy == SyncConfiguration.TimeStampStrategyEnum.GlobalTimeStamp)
            {
                long nowTicks = GetNowTicks();
                if (!IsServerEngine())
                {
                    long lastSync = GetClientLastSync();
                    if (nowTicks <= lastSync) throw new SyncEngineConstraintException("System Date and Time is older than the lastSync value");
                }
                data.GetType().GetProperty(schemaInfo.PropertyInfoDeleted.Name).SetValue(data, nowTicks);
            }
            if (SyncConfiguration.TimeStampStrategy == SyncConfiguration.TimeStampStrategyEnum.DatabaseTimeStamp)
            {
                long timeStamp = GetNextTimeStamp();
                data.GetType().GetProperty(schemaInfo.PropertyInfoDatabaseInstanceId.Name).SetValue(data, null);
                data.GetType().GetProperty(schemaInfo.PropertyInfoLastUpdated.Name).SetValue(data, timeStamp);
                data.GetType().GetProperty(schemaInfo.PropertyInfoDeleted.Name).SetValue(data, true);
            }
        }
    }
}