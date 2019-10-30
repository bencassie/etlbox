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
    public class Sort<TInput> : DataFlowTask, ITask, IDataFlowLinkTarget<TInput>, IDataFlowLinkSource<TInput>, IDataFlowTask<TInput>
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

        public void LinkTo(IDataFlowLinkTarget<TInput> target)
        {
            BlockTransformation.LinkTo(target);
        }

        public void LinkTo(IDataFlowLinkTarget<TInput> target, Predicate<TInput> predicate)
        {
            BlockTransformation.LinkTo(target, predicate);
        }

        IDataFlowTask<TInput> IDataFlowTask<TInput>.Link(IDataFlowLinkTarget<TInput> target)
        {
            this.LinkTo(target);
            return target as IDataFlowTask<TInput>;
        }

        IDataFlowTask<TInput> IDataFlowTask<TInput>.Link(IDataFlowLinkTarget<TInput> target, Predicate<TInput> predicate)
        {
            this.LinkTo(target, predicate);
            return target as IDataFlowTask<TInput>;
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
