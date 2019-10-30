﻿using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace ALE.ETLBox.DataFlow
{
    public abstract class DataFlowSource<TOutput> : DataFlowTask, ITask, IDataFlowTask<TOutput>
    {
        public ISourceBlock<TOutput> SourceBlock => this.Buffer;
        internal BufferBlock<TOutput> Buffer { get; set; } = new BufferBlock<TOutput>();
        internal TypeInfo TypeInfo { get; set; }

        public DataFlowSource()
        {
            TypeInfo = new TypeInfo(typeof(TOutput));
        }

        public abstract void PostAll();

        public async Task StartPostAll()
        {
            var task = Task.Factory.StartNew(
                () => PostAll());
            await task;
        }

        public void LinkTo(IDataFlowLinkTarget<TOutput> target)
        {
            Buffer.LinkTo(target.TargetBlock, new DataflowLinkOptions() { PropagateCompletion = true });
            if (!DisableLogging)
                NLogger.Debug(TaskName + " was linked to Target!", TaskType, "LOG", TaskHash, ControlFlow.ControlFlow.STAGE, ControlFlow.ControlFlow.CurrentLoadProcess?.LoadProcessKey);
        }

        public void LinkTo(IDataFlowLinkTarget<TOutput> target, Predicate<TOutput> predicate)
        {
            Buffer.LinkTo(target.TargetBlock, new DataflowLinkOptions() { PropagateCompletion = true }, predicate);
            if (!DisableLogging)
                NLogger.Debug(TaskName + " was linked to Target!", TaskType, "LOG", TaskHash, ControlFlow.ControlFlow.STAGE, ControlFlow.ControlFlow.CurrentLoadProcess?.LoadProcessKey);
        }

        internal void NLogStart()
        {
            if (!DisableLogging)
                NLogger.Info(TaskName, TaskType, "START", TaskHash, ControlFlow.ControlFlow.STAGE, ControlFlow.ControlFlow.CurrentLoadProcess?.LoadProcessKey);
        }

        internal void NLogFinish()
        {
            if (!DisableLogging && HasLoggingThresholdRows)
                NLogger.Info(TaskName + $" processed {ProgressCount} records in total.", TaskType, "LOG", TaskHash, ControlFlow.ControlFlow.STAGE, ControlFlow.ControlFlow.CurrentLoadProcess?.LoadProcessKey);
            if (!DisableLogging)
                NLogger.Info(TaskName, TaskType, "END", TaskHash, ControlFlow.ControlFlow.STAGE, ControlFlow.ControlFlow.CurrentLoadProcess?.LoadProcessKey);
        }

        internal void LogProgress(int rowsProcessed)
        {
            ProgressCount += rowsProcessed;
            if (!DisableLogging && HasLoggingThresholdRows && (ProgressCount % LoggingThresholdRows == 0))
                NLogger.Info(TaskName + $" processed {ProgressCount} records.", TaskType, "LOG", TaskHash, ControlFlow.ControlFlow.STAGE, ControlFlow.ControlFlow.CurrentLoadProcess?.LoadProcessKey);
        }

        IDataFlowTask<TOutput> IDataFlowTask<TOutput>.Link(IDataFlowLinkTarget<TOutput> target)
        {
            this.LinkTo(target);
            return target as IDataFlowTask<TOutput>;
        }

        IDataFlowTask<TOutput> IDataFlowTask<TOutput>.Link(IDataFlowLinkTarget<TOutput> target, Predicate<TOutput> predicate)
        {

            this.LinkTo(target, predicate);
            return target as IDataFlowTask<TOutput>;
        }

        public IDataFlowDestination<TOutput> Link(IDataFlowDestination<TOutput> target)
        {
            this.LinkTo(target);
            return target as IDataFlowDestination<TOutput>;
        }

        public IDataFlowDestination<TOutput> Link(IDataFlowDestination<TOutput> target, Predicate<TOutput> predicate)
        {
            this.LinkTo(target, predicate);
            return target as IDataFlowDestination<TOutput>;
        }
    }
}
