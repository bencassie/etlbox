﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks.Dataflow;
using CF = ALE.ETLBox.ControlFlow;

namespace ALE.ETLBox.DataFlow
{
    public class DataFlowLinker<TOutput>
    {
        public ISourceBlock<TOutput> SourceBlock { get; set; }
        public bool DisableLogging { get; set; }
        public NLog.Logger NLogger { get; set; } = CF.ControlFlow.GetLogger();
        public ITask CallingTask { get; set; }

        public DataFlowLinker(ITask callingTask, ISourceBlock<TOutput> sourceBlock, bool disableLogging)
        {
            this.CallingTask = callingTask;
            this.SourceBlock = sourceBlock;
            this.DisableLogging = disableLogging;
        }

        public IDataFlowLinkSource<TOutput> LinkTo(IDataFlowLinkTarget<TOutput> target)
            => LinkTo<TOutput>(target);

        public IDataFlowLinkSource<TConvert> LinkTo<TConvert>(IDataFlowLinkTarget<TOutput> target)
        {
            SourceBlock.LinkTo(target.TargetBlock, new DataflowLinkOptions() { PropagateCompletion = true });
            if (!DisableLogging)
                NLogger.Debug(CallingTask.TaskName + $" was linked to: {((ITask)target).TaskName}", CallingTask.TaskType, "LOG", CallingTask.TaskHash, ControlFlow.ControlFlow.STAGE, ControlFlow.ControlFlow.CurrentLoadProcess?.Id);
            return target as IDataFlowLinkSource<TConvert>;
        }

        public IDataFlowLinkSource<TOutput> LinkTo(IDataFlowLinkTarget<TOutput> target, Predicate<TOutput> predicate)
            => LinkTo<TOutput>(target, predicate);

        public IDataFlowLinkSource<TConvert> LinkTo<TConvert>(IDataFlowLinkTarget<TOutput> target, Predicate<TOutput> predicate)
        {
            SourceBlock.LinkTo(target.TargetBlock, new DataflowLinkOptions() { PropagateCompletion = true }, predicate);
            if (!DisableLogging)
                NLogger.Debug(CallingTask.TaskName + $" was linked to (with predicate): {((ITask)target).TaskName}!", CallingTask.TaskType, "LOG", CallingTask.TaskHash, ControlFlow.ControlFlow.STAGE, ControlFlow.ControlFlow.CurrentLoadProcess?.Id);
            return target as IDataFlowLinkSource<TConvert>;
        }

        public IDataFlowLinkSource<TOutput> LinkTo(IDataFlowLinkTarget<TOutput> target, Predicate<TOutput> rowsToKeep, Predicate<TOutput> rowsIntoVoid)
            => LinkTo<TOutput>(target, rowsToKeep, rowsIntoVoid);

        public IDataFlowLinkSource<TConvert> LinkTo<TConvert>(IDataFlowLinkTarget<TOutput> target, Predicate<TOutput> rowsToKeep, Predicate<TOutput> rowsIntoVoid)
        {
            SourceBlock.LinkTo(target.TargetBlock, new DataflowLinkOptions() { PropagateCompletion = true }, rowsToKeep);
            if (!DisableLogging)
                NLogger.Debug(CallingTask.TaskName + $" was linked to (with predicate): {((ITask)target).TaskName}!", CallingTask.TaskType, "LOG", CallingTask.TaskHash, ControlFlow.ControlFlow.STAGE, ControlFlow.ControlFlow.CurrentLoadProcess?.Id);

            SourceBlock.LinkTo<TOutput>(new VoidDestination<TOutput>().TargetBlock, rowsIntoVoid);
            if (!DisableLogging)
                NLogger.Debug(CallingTask.TaskName + $" was also linked to: VoidDestination to ignore certain rows!", CallingTask.TaskType, "LOG", CallingTask.TaskHash, ControlFlow.ControlFlow.STAGE, ControlFlow.ControlFlow.CurrentLoadProcess?.Id);

            return target as IDataFlowLinkSource<TConvert>;
        }
    }
}
