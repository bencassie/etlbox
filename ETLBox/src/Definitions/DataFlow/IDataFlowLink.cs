using System;

namespace ALE.ETLBox.DataFlow
{
    //public interface IDataFlowLink
    //{
    //    IDataFlowLink<object> LinkTo(IDataFlowLinkTarget<object> target);
    //    IDataFlowLink<object> LinkTo(IDataFlowLinkTarget<object> target, Predicate<object> predicate);
    //}
    //public interface IDataFlowLink<TInput> : IDataFlowLink<TInput, TInput> { }
    public interface IDataFlowLink<T>// : IDataFlowLink where TInput: class where TOutput: class
    {
        IDataFlowLink<T> LinkTo(IDataFlowLinkTarget<T> target);
        IDataFlowLink<T> LinkTo(IDataFlowLinkTarget<T> target, Predicate<T> predicate);
        IDataFlowLink<TOut> LinkTo<TOut>(IDataFlowLinkTarget<T> target);
        IDataFlowLink<TOut> LinkTo<TOut>(IDataFlowLinkTarget<T> target, Predicate<T> predicate);
    }
}
