﻿using System;
using System.Collections.Generic;
using System.Linq;
using PrtgAPI.Objects.Shared;

namespace PrtgAPI.PowerShell.Base
{
    /// <summary>
    /// Base class for all cmdlets that return a list of objects.
    /// </summary>
    /// <typeparam name="T">The type of objects that will be retrieved.</typeparam>
    public abstract class PrtgObjectCmdlet<T> : PrtgCmdlet
    {
        /// <summary>
        /// Retrieves all records of a specified type from a PRTG Server. Implementors can call different methods of a <see cref="PrtgClient"/> based on the type they wish to retrieve.
        /// </summary>
        /// <returns>A list of records relevant to the caller.</returns>
        protected abstract IEnumerable<T> GetRecords();

        /// <summary>
        /// Provides a record-by-record processing functionality for the cmdlet.
        /// </summary>
        protected override void ProcessRecord()
        {
            var records = GetRecords();

            WriteList(records, null);
        }
    }
}