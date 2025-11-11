using System;

namespace WhiteArrowEditor
{
    public interface IFlexItemCreator
    {
        void RequestCreate(Action<bool> onComplete);
    }
}