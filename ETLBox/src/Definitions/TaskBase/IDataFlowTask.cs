using ALE.ETLBox.DataFlow;
using System;

namespace ALE.ETLBox
{
    public interface IDataFlowTask<T>
    {
        IDataFlowTask<T> Link(IDataFlowLinkTarget<T> target);
        IDataFlowTask<T> Link(IDataFlowLinkTarget<T> target, Predicate<T> predicate);


        IDataFlowDestination<T> Link(IDataFlowDestination<T> target);
        IDataFlowDestination<T> Link(IDataFlowDestination<T> target, Predicate<T> predicate);

        

    }
}