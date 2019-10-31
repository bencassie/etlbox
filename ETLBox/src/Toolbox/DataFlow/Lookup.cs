﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;


namespace ALE.ETLBox.DataFlow
{
    /// <summary>
    /// A lookup task - data from the input can be enriched with data retrieved from the lookup source. The result is then posted into the output.
    /// </summary>
    /// <typeparam name="TTransformationInput">Type of data input</typeparam>
    /// <typeparam name="TTransformationOutput">Type of data output</typeparam>
    /// <typeparam name="TSourceOutput">Type of lookup data</typeparam>
    /// <example>
    /// <code>
    /// Lookup&lt;MyInputDataRow, MyOutputDataRow, MyLookupRow&gt; lookup = new Lookup&lt;MyInputDataRow, MyOutputDataRow,MyLookupRow&gt;(
    ///     testClass.TestTransformationFunc, lookupSource, testClass.LookupData
    /// );
    /// </code>
    /// </example>
    public class Lookup<TTransformationInput, TTransformationOutput, TSourceOutput>
        : DataFlowTask, ITask, IDataFlowTransformation<TTransformationInput, TTransformationOutput>, IDataFlowLink<TTransformationOutput>
    {

        /* ITask Interface */
        public override string TaskName { get; set; } = "Dataflow: Lookup";

        public List<TSourceOutput> LookupList { get; set; }
        ActionBlock<TSourceOutput> LookupBuffer { get; set; }

        /* Public Properties */
        public ISourceBlock<TTransformationOutput> SourceBlock => RowTransformation.SourceBlock;
        public ITargetBlock<TTransformationInput> TargetBlock => RowTransformation.TargetBlock;
        public IDataFlowSource<TSourceOutput> Source
        {
            get
            {
                return _source;
            }
            set
            {
                _source = value;
                Source.SourceBlock.LinkTo(LookupBuffer, new DataflowLinkOptions() { PropagateCompletion = true });
            }
        }

        /* Private stuff */
        RowTransformation<TTransformationInput, TTransformationOutput> RowTransformation { get; set; }

        private Func<TTransformationInput, TTransformationOutput> _rowTransformationFunc;
        private IDataFlowSource<TSourceOutput> _source;

        Func<TTransformationInput, TTransformationOutput> RowTransformationFunc
        {
            get
            {
                return _rowTransformationFunc;
            }
            set
            {
                _rowTransformationFunc = value;
                RowTransformation = new RowTransformation<TTransformationInput, TTransformationOutput>(this, _rowTransformationFunc);
                RowTransformation.InitAction = LoadLookupData;
            }
        }
        public Lookup()
        {
            LookupBuffer = new ActionBlock<TSourceOutput>(row => FillBuffer(row));
        }

        public Lookup(Func<TTransformationInput, TTransformationOutput> rowTransformationFunc, IDataFlowSource<TSourceOutput> source) : this()
        {
            RowTransformationFunc = rowTransformationFunc;
            Source = source;
        }

        public Lookup(Func<TTransformationInput, TTransformationOutput> rowTransformationFunc, IDataFlowSource<TSourceOutput> source, List<TSourceOutput> lookupList) : this()
        {
            RowTransformationFunc = rowTransformationFunc;
            Source = source;
            LookupList = lookupList;
        }


        private void LoadLookupData()
        {
            Source.StartPostAll();
            LookupBuffer.Completion.Wait();
        }

        private void FillBuffer(TSourceOutput sourceRow)
        {
            if (LookupList == null) LookupList = new List<TSourceOutput>();
            LookupList.Add(sourceRow);
        }

        public IDataFlowLinkSource<TTransformationOutput> LinkTo(IDataFlowLinkTarget<TTransformationOutput> target)
        {
            return LinkTo<TTransformationOutput>(target);
        }

        public IDataFlowLinkSource<TTransformationOutput> LinkTo(IDataFlowLinkTarget<TTransformationOutput> target, Predicate<TTransformationOutput> predicate)
        {
            return LinkTo<TTransformationOutput>(target, predicate);
        }

        public IDataFlowLinkSource<TOut> LinkTo<TOut>(IDataFlowLinkTarget<TTransformationOutput> target)
        {
            RowTransformation.LinkTo<TTransformationOutput>(target);
            return target as IDataFlowLinkSource<TOut>;
        }

        public IDataFlowLinkSource<TOut> LinkTo<TOut>(IDataFlowLinkTarget<TTransformationOutput> target, Predicate<TTransformationOutput> predicate)
        {
            RowTransformation.LinkTo<TTransformationOutput>(target, predicate);
            return target as IDataFlowLinkSource<TOut>;
        }

    }

    /// <summary>
    /// A lookup task - data from the input can be enriched with data retrieved from the lookup source.
    /// The non generic implementation accepts a string array as input and output. The lookup data source
    /// always returns a list of string array.
    /// </summary>
    /// <example>
    /// <code>
    /// Lookup = new Lookup(
    ///     testClass.TestTransformationFunc, lookupSource, testClass.LookupData
    /// );
    /// </code>
    /// </example>
    public class Lookup : Lookup<string[], string[], string[]>
    {
        public Lookup() : base()
        { }

        public Lookup(Func<string[], string[]> rowTransformationFunc, IDataFlowSource<string[]> source)
            : base(rowTransformationFunc, source)
        { }

        public Lookup(Func<string[], string[]> rowTransformationFunc, IDataFlowSource<string[]> source, List<string[]> lookupList)
            : base(rowTransformationFunc, source, lookupList)
        { }
    }

}
