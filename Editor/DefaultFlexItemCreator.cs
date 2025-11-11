using System;

namespace WhiteArrowEditor
{
    public class DefaultFlexItemCreator : IFlexItemCreator
    {
        private readonly Action _create;



        public DefaultFlexItemCreator(Action create)
        {
            _create = create ?? throw new ArgumentNullException(nameof(create));
        }



        public void RequestCreate(Action<bool> onComplete)
        {
            _create();
            onComplete(true);
        }
    }
}