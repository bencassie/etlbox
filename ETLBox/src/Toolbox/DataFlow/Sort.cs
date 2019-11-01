﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;


namespace ALE.ETLBox.DataFlow
{
    /// <summary>
    /// Sort the input with the given sort function.
    /// </summary>
    /// <typeparam name="TInput">Type of input data (equal type of output data).</typeparam>
    /// <example>
    /// <code>
    /// Comparison&lt;MyDataRow&gt; comp = new Comparison&lt;MyDataRow&gt;(
    ///     (x, y) => y.Value2 - x.Value2
    /// );
    /// Sort&lt;MyDataRow&gt; block = new Sort&lt;MyDataRow&gt;(comp);
    /// </code>
    /// </example>
    public class Sort<TInput> : DataFlowTask, ITask, IDataFlowLinkTarget<TInput>, IDataFlowLinkSource<TInput>
    {


        /* ITask Interface */
        public override string TaskName { get; set; } = "Dataflow: Sort";

        /* Public Properties */

        public Comparison<TInput> SortFunction
        {
            get { return _sortFunction; }
            set
            {
                _sortFunction = value;
                BlockTransformation = new BlockTransformation<TInput>(this, SortByFunc);
            }
        }

        public ISourceBlock<TInput> SourceBlock => BlockTransformation.SourceBlock;
        public ITargetBlock<TInput> TargetBlock => BlockTransformation.TargetBlock;

        /* Private stuff */
        Comparison<TInput> _sortFunction;
        BlockTransformation<TInput> BlockTransformation { get; set; }
        public Sort()
        {
            NLogger = NLog.LogManager.GetLogger("ETL");
        }

        public Sort(Comparison<TInput> sortFunction) : this()
        {
            SortFunction = sortFunction;
        }

        public Sort(string name, Comparison<TInput> sortFunction) : this(sortFunction)
        {
            this.TaskName = name;
        }

        List<TInput> SortByFunc(List<TInput> data)
        {
            data.Sort(SortFunction);
            return data;
        }

        public IDataFlowLinkSource<TInput> LinkTo(IDataFlowLinkTarget<TInput> target)
        {
            return LinkTo<TInput>(target);
        }

        public IDataFlowLinkSource<TInput> LinkTo(IDataFlowLinkTarget<TInput> target, Predicate<TInput> predicate, bool alsoNegatePredicateWithVoidDestination = false)
        {
            return LinkTo<TInput>(target, predicate, alsoNegatePredicateWithVoidDestination);
        }

        public IDataFlowLinkSource<TOut> LinkTo<TOut>(IDataFlowLinkTarget<TInput> target)
        {
            BlockTransformation.LinkTo<TInput>(target);
            if (!DisableLogging)
                NLogger.Debug(TaskName + " was linked to Target!", TaskType, "LOG", TaskHash, ControlFlow.ControlFlow.STAGE, ControlFlow.ControlFlow.CurrentLoadProcess?.LoadProcessKey);
            return target as IDataFlowLinkSource<TOut>;
        }

        public IDataFlowLinkSource<TOut> LinkTo<TOut>(IDataFlowLinkTarget<TInput> target, Predicate<TInput> predicate, bool alsoNegatePredicateWithVoidDestination = false)
        {
            BlockTransformation.LinkTo<TInput>(target, predicate);
            if (!DisableLogging)
                NLogger.Debug(TaskName + " was linked to Target!", TaskType, "LOG", TaskHash, ControlFlow.ControlFlow.STAGE, ControlFlow.ControlFlow.CurrentLoadProcess?.LoadProcessKey);
            if (alsoNegatePredicateWithVoidDestination)
            {
                BlockTransformation.LinkTo<TInput>(new VoidDestination<TInput>(), x => !predicate(x));
                if (!DisableLogging)
                    NLogger.Debug(TaskName + " was also linked to VoidDestination with negative predicate!", TaskType, "LOG", TaskHash, ControlFlow.ControlFlow.STAGE, ControlFlow.ControlFlow.CurrentLoadProcess?.LoadProcessKey);
            }
            return target as IDataFlowLinkSource<TOut>;
        }
    }

    /// <summary>
    /// Sort the input with the given sort function. The non generic implementation works with string array.
    /// </summary>
    public class Sort : Sort<string[]>
    {
        public Sort() : base()
        { }

        public Sort(Comparison<string[]> sortFunction) : base(sortFunction)
        { }

        public Sort(string name, Comparison<string[]> sortFunction) : base(name, sortFunction)
        { }
    }


}
