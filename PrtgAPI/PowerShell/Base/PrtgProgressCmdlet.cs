﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using PrtgAPI.Objects.Shared;
using PrtgAPI.PowerShell.Progress;

namespace PrtgAPI.PowerShell.Base
{
    /// <summary>
    /// Base class for all cmdlets that display progress while reading and writing to the pipeline.
    /// </summary>
    public abstract class PrtgProgressCmdlet : PrtgCmdlet
    {
        /// <summary>
        /// Description of the object type output by this cmdlet.
        /// </summary>
        internal string TypeDescription;

        internal string OperationTypeDescription;

        /// <summary>
        /// Initializes a new instance of the <see cref="PrtgProgressCmdlet"/> class.
        /// </summary>
        /// <param name="typeDescription"></param>
        protected PrtgProgressCmdlet(string typeDescription)
        {
            TypeDescription = typeDescription;
        }

        /// <summary>
        /// Write a basic PSObject to the pipeline while displaying an appropriate progress message.
        /// </summary>
        /// <param name="obj">The object to write to the pipeline.</param>
        protected void WriteObjectWithProgress(object obj)
        {
            DisplayProgress();

            WriteObject(obj);

            PostUpdateProgress();
        }

        internal void DisplayProgress(bool writeObject = true)
        {
            if (ProgressManager.GetRecordsWithVariableProgress)
                UpdatePreviousAndCurrentVariableProgressOperations(writeObject);
            else if (ProgressManager.GetResultsWithProgress)
            {
                if (ProgressManager.PreviousContainsProgress)
                {
                    ProgressManager.SetPreviousOperation($"Retrieving all {GetTypePlural()}");
                }
            }
        }

        internal void DisplayProcessProgress(int percentage)
        {
            if (!ProgressManager.ProgressEnabled)
                return;

            if (ProgressManager.PreviousRecord != null)
                ProgressManager.RemovePreviousOperation();

            if (ProgressManager.GetRecordsWithVariableProgress)
            {
                UpdatePreviousAndCurrentVariableProgressOperations(percentage == 100, $"Probing target device ({percentage}%)");
            }
            else
            {
                ProgressManager.CurrentRecord.PercentComplete = percentage;

                ProgressManager.WriteProgress($"PRTG {TypeDescription} Search", $"Probing target device ({percentage}%)");
            }

            if (percentage == 100)
            {
                //If we're Variable -> Action -> Me, we wrote all our progress to the previous cmdlet's CurrentOperation,
                //so we don't need to complete anything
                if (!(ProgressManager.PipeFromVariableWithProgress && ProgressManager.PipelineContainsOperation))
                {
                    if (ProgressManager.ReadyToComplete())
                        ProgressManager.CompleteProgress(true);

                    if(!ProgressManager.GetRecordsWithVariableProgress)
                        ProgressManager.MaybeCompletePreviousProgress();
                }
            }
            else
            {
                //Null out the previous record's operation so it doesn't get removed on the next request
                if (ProgressManager.PreviousRecord != null && ProgressManager.PreviousRecord.CurrentOperation != null)
                    ProgressManager.PreviousRecord.CurrentOperation = null;
            }
        }

        /// <summary>
        /// Updates the previous and current progress records for the current input object for scenarios in which a variable was piped into one or more cmdlets.
        /// </summary>
        protected void UpdatePreviousAndCurrentVariableProgressOperations(bool writeObject = true, string currentOperation = null)
        {
            //PrtgOperationCmdlets use TrySetPreviousOperation, so they won't be caught by the fact PipelineContainsOperation would be true for them
            //(causing them to SetPreviousOperation, which would fail)
            if (!ProgressManager.PipelineContainsOperation)
            {
                if (ProgressManager.PipelineIsProgressPure)
                {
                    ProgressManager.CurrentRecord.Activity = $"PRTG {TypeDescription} Search";

                    var obj = ProgressManager.CmdletPipeline.Current;
                    var prtgObj = obj as PrtgObject;

                    if (prtgObj != null)
                        ProgressManager.InitialDescription = $"Processing {GetTypeDescription(prtgObj.GetType()).ToLower()}";
                    else
                        ProgressManager.InitialDescription = $"Processing all {GetTypeDescription(obj.GetType()).ToLower()}s";
                    ProgressManager.CurrentRecord.CurrentOperation = currentOperation ?? $"Retrieving all {GetTypePlural()}";

                    ProgressManager.RemovePreviousOperation();
                    ProgressManager.UpdateRecordsProcessed(ProgressManager.CurrentRecord, prtgObj, writeObject);
                }
            }
            else
                ProgressManager.SetPreviousOperation(currentOperation ?? $"Retrieving all {GetTypePlural()}");
        }

        /// <summary>
        /// Updates progress before any of the objects in the current cmdlet are written to the pipeline.
        /// </summary>
        protected void PreUpdateProgress()
        {
            switch (GetSwitchScenario())
            {
                case ProgressScenario.NoProgress:
                    break;
                case ProgressScenario.StreamProgress:
                case ProgressScenario.MultipleCmdlets:
                    UpdateScenarioProgress_MultipleCmdlets(ProgressStage.PreLoop, null);
                    break;
                case ProgressScenario.VariableToSingleCmdlet:
                    break;
                case ProgressScenario.VariableToMultipleCmdlets:
                case ProgressScenario.MultipleCmdletsFromBlockingSelect:
                    UpdateScenarioProgress_VariableToMultipleCmdlet(ProgressStage.PreLoop, null);
                    break;
                default:
                    throw new NotImplementedException($"Handler for ProgressScenario '{ProgressManager.Scenario}' is not implemented");
            }
        }

        /// <summary>
        /// Updates progress as each object in the current cmdlet is written to the pipeline.
        /// </summary>
        protected void DuringUpdateProgress(PrtgObject obj)
        {
            switch (GetSwitchScenario())
            {
                case ProgressScenario.NoProgress:
                    break;
                case ProgressScenario.StreamProgress:
                case ProgressScenario.MultipleCmdlets:
                    UpdateScenarioProgress_MultipleCmdlets(ProgressStage.BeforeEach, obj);
                    break;
                case ProgressScenario.VariableToSingleCmdlet:
                    ProgressManager.CompletePrematurely(ProgressManager);
                    break;
                case ProgressScenario.VariableToMultipleCmdlets:
                case ProgressScenario.MultipleCmdletsFromBlockingSelect:
                    UpdateScenarioProgress_VariableToMultipleCmdlet(ProgressStage.BeforeEach, obj);
                    break;
                case ProgressScenario.SelectLast:
                case ProgressScenario.SelectSkipLast:
                    if (ProgressManager.PartOfChain)
                        UpdateScenarioProgress_VariableToMultipleCmdlet(ProgressStage.BeforeEach, obj);
                    break;
                default:
                    throw new NotImplementedException($"Handler for ProgressScenario '{ProgressManager.Scenario}' is not implemented");
            }
        }

        /// <summary>
        /// Updates progress after all objects in the current cmdlet have been written to the pipeline.
        /// </summary>
        protected void PostUpdateProgress()
        {
            switch (GetSwitchScenario())
            {
                case ProgressScenario.NoProgress:
                    break;
                case ProgressScenario.StreamProgress:
                case ProgressScenario.MultipleCmdlets:
                    UpdateScenarioProgress_MultipleCmdlets(ProgressStage.PostLoop, null);
                    break;
                case ProgressScenario.VariableToSingleCmdlet:
                    UpdateScenarioProgress_VariableToSingleCmdlet(ProgressStage.PostLoop);
                    break;
                case ProgressScenario.VariableToMultipleCmdlets:
                case ProgressScenario.MultipleCmdletsFromBlockingSelect:
                    UpdateScenarioProgress_VariableToMultipleCmdlet(ProgressStage.PostLoop, null);
                    break;
                default:
                    throw new NotImplementedException($"Handler for ProgressScenario '{ProgressManager.Scenario}' is not implemented");
            }
        }

        private ProgressScenario GetSwitchScenario()
        {
            var scenario = ProgressManager.Scenario;

            if (scenario == ProgressScenario.SelectLast || scenario == ProgressScenario.SelectSkipLast)
            {
                scenario = ProgressManager.CalculateNonBlockingProgressScenario();

                if (scenario == ProgressScenario.NoProgress)
                    scenario = ProgressScenario.VariableToSingleCmdlet;
                else if (scenario == ProgressScenario.MultipleCmdlets) //We don't want MultipleCmdlets, because that makes us responsible for updating the records processed on each iteration
                    scenario = ProgressScenario.VariableToMultipleCmdlets;

                if (ProgressManager.Scenario == ProgressScenario.SelectSkipLast)
                {
                    //We can't trust PartOfChain as the cmdlets before us haven't been destroyed yet. So instead,
                    //the question is are there any cmdlets after us?

                    if (ProgressManager.LastInChain)
                        scenario = ProgressScenario.VariableToSingleCmdlet;
                }
            }

            return scenario;
        }

        private void UpdateScenarioProgress_MultipleCmdlets(ProgressStage stage, PrtgObject obj)
        {
            if (ProgressManager.ExpectsContainsProgress)
            {
                if (stage == ProgressStage.PreLoop)
                {
                    ProgressManager.RemovePreviousOperation();
                }
                else if (stage == ProgressStage.BeforeEach)
                {
                    ProgressManager.UpdateRecordsProcessed(ProgressManager.CurrentRecord, obj);
                }
                else //PostLoop
                {
                    ProgressManager.CompleteProgress();
                }
            }

            if (stage == ProgressStage.PostLoop)
                ProgressManager.MaybeCompletePreviousProgress();
        }

        private void UpdateScenarioProgress_VariableToSingleCmdlet(ProgressStage stage)
        {
            if (stage == ProgressStage.PreLoop)
            {
            }
            else if (stage == ProgressStage.BeforeEach)
            {
            }
            else //PostLoop
            {
                if (ProgressManager.ExpectsContainsProgress)
                    ProgressManager.CompleteProgress();
            }
        }

        private void UpdateScenarioProgress_VariableToMultipleCmdlet(ProgressStage stage, PrtgObject obj)
        {
            if (stage == ProgressStage.PreLoop)
            {
                if (ProgressManager.PipelineContainsOperation)
                {
                    if (!ProgressManager.LastInChain)
                        SetObjectSearchProgress(ProcessingOperation.Processing);

                    if (ProgressManager.ExpectsContainsProgress)
                        ProgressManager.RemovePreviousOperation();
                }
            }
            else if (stage == ProgressStage.BeforeEach)
            {
                //When a variable to cmdlet chain contains an operation, responsibility of updating the number of records processed
                //"resets", and we become responsible for updating our own count again
                if (ProgressManager.PipelineContainsOperation && ProgressManager.ExpectsContainsProgress)
                    ProgressManager.UpdateRecordsProcessed(ProgressManager.CurrentRecord, obj);
            }
            else //PostLoop
            {
                UpdateScenarioProgress_VariableToSingleCmdlet(stage);
            }
        }

        internal void UpdatePreviousProgressAndSetCount(int count)
        {
            ProgressManager.TotalRecords = count;

            if (!ProgressManager.LastInChain)
            {
                SetObjectSearchProgress(ProcessingOperation.Processing);
            }
        }

        /// <summary>
        /// Set the progress activity, initial description and total number of records (where applicable) for the current cmdlet.
        /// </summary>
        /// <param name="operation">The type of processing that is being performed by this cmdlet.</param>
        protected void SetObjectSearchProgress(ProcessingOperation operation)
        {
            ProgressManager.CurrentRecord.Activity = $"PRTG {TypeDescription} Search";

            if (operation == ProcessingOperation.Processing)
                ProgressManager.InitialDescription = $"Processing {TypeDescription.ToLower()}";
            else
                ProgressManager.InitialDescription = $"Retrieving all {GetTypePlural()}";
        }

        /// <summary>
        /// Writes a list to the output pipeline.
        /// </summary>
        /// <param name="sendToPipeline">The list that will be output to the pipeline.</param>
        internal void WriteList<T>(IEnumerable<T> sendToPipeline)
        {
            PreUpdateProgress();

            foreach (var item in sendToPipeline)
            {
                DuringUpdateProgress(item as PrtgObject);

                WriteObject(item);
            }

            PostUpdateProgress();
        }

        /// <summary>
        /// Retrieves the value of a <see cref="DescriptionAttribute"/> of the specified type. If the type does not have a <see cref="DescriptionAttribute"/>, its name is used instead.
        /// </summary>
        /// <param name="type">The type whose description should be retrieved.</param>
        /// <returns>The type's name or description.</returns>
        protected static string GetTypeDescription(Type type)
        {
            var attribute = type.GetCustomAttribute<DescriptionAttribute>();

            if (attribute != null)
                return attribute.Description;

            return type.Name;
        }

        private string GetTypePlural()
        {
            var str = OperationTypeDescription?.ToLower() ?? TypeDescription.ToLower();

            if (str.EndsWith("ies"))
                return str;

            return $"{str}s";
        }

        /// <summary>
        /// Create a sequence of progress tasks for executing a process containing two or more operations.
        /// </summary>
        /// <typeparam name="TResult">The type of object returned by the first operation.</typeparam>
        /// <param name="func">The first operation to execute.</param>
        /// <param name="typeDescription">The type description use for the progress.</param>
        /// <param name="operationDescription">The progress description to use for the first operation.</param>
        /// <returns></returns>
        protected ProgressTask<TResult> First<TResult>(Func<List<TResult>> func, string typeDescription, string operationDescription)
        {
            return ProgressTask<TResult>.Create(func, this, typeDescription, operationDescription);
        }
    }
}
