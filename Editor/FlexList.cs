using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace WhiteArrowEditor
{
    public class FlexList : VisualElement
    {
        private readonly VisualTreeAsset _listXml;
        private readonly VisualTreeAsset _itemXml;
        private readonly VisualTreeAsset _actionButtonXml;



        private IEnumerable<object> _itemsSource;
        private IFlexItemCreator _itemSourceCreator;
        private Action<object> _removeSourceItem;

        private bool _isItemReorderingEnabled;
        private Action<object, int> _reorderSourceItem;



        private readonly VisualElement _headerContainer;
        private readonly Label _label;
        private readonly Button _addButton;

        private readonly VisualElement _itemsContainer;
        private readonly Dictionary<object, bool> _foldoutStates = new();

        public Func<object, string> GetItemName;
        public Func<object, IEnumerable<Button>> OnCreateActionButtons { get; set; }
        private Func<object, VisualElement> _renderItem;



        public Label Label => _label;
        public bool IsItemReorderingEnabled => _isItemReorderingEnabled;



        public event Action Changed;
        public event Action PreRefresh;




        public FlexList()
        {
            _listXml = LoadUXML("list.uxml");
            _itemXml = LoadUXML("list_element.uxml");
            _actionButtonXml = LoadUXML("list_element_actionButton.uxml");

            var listRoot = _listXml.CloneTree();
            Add(listRoot);

            _itemsContainer = this.Q("elements-container");
            _label = listRoot.Q<Label>("list-label");

            _addButton = listRoot.Q<Button>("addElement-button");
            _addButton.clicked += () =>
            {
                _itemSourceCreator.RequestCreate(result =>
                {
                    if (result)
                    {
                        Changed?.Invoke();
                        Refresh();
                    }
                });
            };

            _headerContainer = _addButton.parent;
        }

        private VisualTreeAsset LoadUXML(string relativePath)
        {
            var fromPackage = EditorGUIUtility.Load($"Packages/com.white-arrow-editor.flex-list/UXML/{relativePath}") as VisualTreeAsset;
            if (fromPackage != null)
                return fromPackage;

            // Fallback
            var fromAssets = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"Assets/editor-flex-list/UXML/{relativePath}");
            if (fromAssets != null)
                return fromAssets;

            Debug.LogWarning($"[FlexList] Could not find UXML: {relativePath}");
            return null;
        }



        public void AddHeaderButton(string text, int width, Action onClick)
        {
            var btn = CreateActionButton(text, onClick);
            btn.style.width = width;
            AddCustomHeaderElement(btn);
        }

        public void AddCustomHeaderElement(VisualElement element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            var index = _headerContainer.IndexOf(_addButton);
            if (index < 0)
                _headerContainer.Add(element);
            else
                _headerContainer.Insert(index, element);
        }



        public void SetItemsSource(
            IEnumerable<object> source,
            Action createItemSource,
            Action<object> removeSourceItem,
            Func<object, VisualElement> renderItem)
        {
            SetItemsSource(
                source,
                new DefaultFlexItemCreator(createItemSource),
                removeSourceItem,
                renderItem
            );
        }

        public void SetItemsSource(
            IEnumerable<object> source,
            IFlexItemCreator itemSourceCreator,
            Action<object> removeSourceItem,
            Func<object, VisualElement> renderItem)
        {
            _itemsSource = source ?? throw new ArgumentNullException(nameof(source));
            _itemSourceCreator = itemSourceCreator ?? throw new ArgumentNullException(nameof(itemSourceCreator));
            _removeSourceItem = removeSourceItem ?? throw new ArgumentNullException(nameof(removeSourceItem));
            _renderItem = renderItem ?? throw new ArgumentNullException(nameof(renderItem));
        }



        public void EnableItemReordering(Action<object, int> reorderItemSource)
        {
            _reorderSourceItem = reorderItemSource ?? throw new ArgumentNullException(nameof(reorderItemSource));
            _isItemReorderingEnabled = true;
        }

        public void DisableItemReordering()
        {
            _isItemReorderingEnabled = false;
            _reorderSourceItem = null;
        }



        public void Refresh()
        {
            PreRefresh?.Invoke();

            CacheFoldoutStates();
            _itemsContainer.Clear();

            if (_itemsSource == null || _itemsSource.Count() == 0)
            {
                var label = new Label("Flex List is empty!")
                {
                    style =
                    {
                        marginBottom = 6,
                        marginTop = 6,
                        marginLeft = 6,
                        marginRight = 6
                    }
                };
                _itemsContainer.Add(label);
                return;
            }

            for (int i = 0; i < _itemsSource.Count(); i++)
            {
                var item = _itemsSource.ElementAt(i);
                var name = GetItemDisplayName(item);
                CreateItemFoldout(item, name);
            }

            RestoreFoldoutStates();
        }



        public void RefreshItemDisplayNames()
        {
            if (_itemsSource == null)
                return;

            var items = _itemsSource.ToList();
            var foldouts = _itemsContainer.Query<Foldout>().ToList();

            for (int i = 0; i < items.Count && i < foldouts.Count; i++)
            {
                var item = items[i];
                var name = GetItemDisplayName(item);
                foldouts[i].text = name;
            }
        }

        public void RefreshItemDisplayName(object item)
        {
            foreach (var foldout in _itemsContainer.Query<Foldout>().ToList())
            {
                if (Equals(foldout.userData, item))
                {
                    var name = GetItemDisplayName(item);
                    foldout.text = name;
                    break;
                }
            }
        }

        private string GetItemDisplayName(object item)
        {
            var itemIdex = _itemsSource.ToList().IndexOf(item);
            var name = GetItemName?.Invoke(item) ?? $"Element {itemIdex}";
            return name;
        }



        private void CreateItemFoldout(object item, string name)
        {
            var element = _itemXml.CloneTree();
            _itemsContainer.Add(element);

            var foldout = element.Q<Foldout>("element-foldout");
            foldout.text = name;
            foldout.userData = item;
            foldout.contentContainer.style.paddingTop = 6;

            var toggle = foldout.Q<Toggle>();
            toggle.style.marginLeft = 0;
            toggle.style.marginTop = 0;
            toggle.style.marginBottom = 0;
            toggle.style.backgroundColor = new Color(0.2352941F, 0.2352941F, 0.2352941F);

            var header = toggle?.Q<Label>()?.parent;
            CreateButtonGroup(item, header);

            var customContent = _renderItem.Invoke(item);
            if (customContent == null)
                foldout.Add(new Label("No content"));
            else
                foldout.Add(customContent);
        }



        private void CreateButtonGroup(object item, VisualElement container)
        {
            container.AddRange(CreateCustomActionButtons(item));

            if (_isItemReorderingEnabled)
            {
                var index = _itemsSource.ToList().IndexOf(item);
                container.AddRange(CreateReorderItemButtons(item, index));
            }

            container.Add(CreateRemoveItemButton(item));
        }

        private IEnumerable<Button> CreateCustomActionButtons(object item)
        {
            var customButtons = OnCreateActionButtons?.Invoke(item);
            return customButtons ?? Enumerable.Empty<Button>();
        }

        private Button CreateRemoveItemButton(object item)
        {
            var removeButton = CreateActionButton("✖", () =>
            {
                if (_itemsSource != null && _itemsSource.Contains(item))
                {
                    _removeSourceItem(item);
                    Changed?.Invoke();
                    Refresh();
                }
            });

            removeButton.style.backgroundColor = Color.red;
            return removeButton;
        }

        private IEnumerable<Button> CreateReorderItemButtons(object item, int index)
        {
            var moveToUpButton = CreateActionButton("↑",
                () => MoveSourceItem(item, index - 1));

            var moveToDownButton = CreateActionButton("↓",
                () => MoveSourceItem(item, index + 1));

            return new[] { moveToUpButton, moveToDownButton };
        }

        public Button CreateActionButton(string text, Action onClick)
        {
            var container = _actionButtonXml.CloneTree();
            var button = container.Q<Button>();
            button.text = text;
            button.clicked += onClick;
            return button;
        }



        private void CacheFoldoutStates()
        {
            _foldoutStates.Clear();
            foreach (var foldout in _itemsContainer.Query<Foldout>().ToList())
            {
                var item = foldout.userData;
                if (item != null)
                    _foldoutStates[item] = foldout.value;
            }
        }

        private void RestoreFoldoutStates()
        {
            foreach (var foldout in _itemsContainer.Query<Foldout>().ToList())
            {
                var item = foldout.userData;
                if (item != null && _foldoutStates.ContainsKey(item))
                    foldout.value = _foldoutStates[item];
            }
        }



        private void MoveSourceItem(object item, int newIndex)
        {
            var maxIndex = Math.Max(0, _itemsSource.Count() - 1);
            var targetIndex = Math.Max(0, Math.Min(newIndex, maxIndex));

            _reorderSourceItem(item, targetIndex);
            Changed?.Invoke();
            Refresh();
        }
    }
}