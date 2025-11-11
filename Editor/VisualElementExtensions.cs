using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace WhiteArrowEditor
{
    public static class VisualElementExtensions
    {
        public static void AddRange(this VisualElement container, IEnumerable<VisualElement> elements)
        {
            if (elements == null)
                throw new ArgumentNullException(nameof(elements));

            foreach (var element in elements)
            {
                if (element != null)
                    container.Add(element);
            }
        }
    }
}