# FlexList

A flexible **UI Toolkit** list for Unity editor tools. It is a reusable `VisualElement` that renders collections, supports custom item UI, optional reordering, custom per‑item actions, and stable foldout states.

## Features
- Bind to any enumerable source; plug in creation/removal logic.
- Custom per‑item rendering (IMGUI or UI Toolkit).
- Optional item reordering via Up/Down actions.
- Custom action buttons per item (with a helper to create styled buttons).
- Optional custom item naming; defaults to `Element {index}`.
- Drop‑in `VisualElement` usage (add to any parent with `parent.Add(flexList)`).
- Zero hard ties to your data types.

## Installing
Install via **Unity Package Manager** → *Add package from git URL…*:

```
https://github.com/CliffCalist/editor-flex-list.git
```

## Usage

### Basic setup
```csharp
// FlexList is a VisualElement → you can attach it to any parent container.
var flexList = new FlexList();
parentVisualElement.Add(flexList);

// Define how to create, remove, and render items.
// These delegates keep FlexList decoupled from your concrete types.
Action createItem = () =>
{
    // Create and add a new item to your collection
    myItemList.Add(new MyItem());
};

Action<object> removeItem = item =>
{
    // Remove the provided item from your collection
    myItemList.Remove((MyItem)item);
};

Func<object, VisualElement> renderItem = item =>
{
    // Render each item (IMGUI shown here, UI Toolkit works as well)
    var editor = UnityEditor.Editor.CreateEditor((MyItem)item);
    return new IMGUIContainer(() => editor.OnInspectorGUI());
};

// Bind the source and behavior
flexList.SetItemsSource(
    myItemList,   // IEnumerable / IList source
    createItem,   // how to create a new item
    removeItem,   // how to remove an item
    renderItem    // how to render an item
);

// When your data changes, call Refresh() to redraw the list.
flexList.Refresh();
```

### Item reordering (optional)
By enabling reordering you **provide a delegate** that performs the actual move inside your collection. The list will call it with the target index.

```csharp
// Enable Up/Down actions; you control how the collection reorders.
flexList.EnableItemReordering((item, targetIndex) =>
{
    var typed = (MyItem)item;
    var from = myItemList.IndexOf(typed);
    if (from < 0 || targetIndex < 0 || targetIndex >= myItemList.Count) return;

    myItemList.RemoveAt(from);
    myItemList.Insert(targetIndex, typed);
});
```

### Custom action buttons
FlexList provides a helper to create buttons styled consistently with the list. You return any number of buttons; they appear in each item header **before** the built‑in remove button.

```csharp
flexList.OnCreateActionButtons = item =>
{
    // Create a base button using FlexList's helper (label + onClick).
    var openBtn = flexList.CreateActionButton("Open", () =>
    {
        Debug.Log("Open clicked for " + item);
    });

    // You can further style the returned Button (colors, classes, tooltips).
    openBtn.tooltip = "Open this item";

    var duplicateBtn = flexList.CreateActionButton("Duplicate", () =>
    {
        var copy = ((MyItem)item).Clone();
        myItemList.Add(copy);
        flexList.Refresh(); // data changed → redraw
    });

    return new[] { openBtn, duplicateBtn };
};
```

### Custom item naming
By default FlexList names items like Unity does: `Element {index}`. Provide a delegate to override this per item.

```csharp
flexList.GetItemName = item =>
{
    var typed = (MyItem)item;
    return string.IsNullOrWhiteSpace(typed.DisplayName)
        ? null // falls back to "Element {index}"
        : typed.DisplayName;
};
```

## Advanced usage

### Custom item creation flow (e.g., GenericMenu)
For complex flows (selecting a subtype, confirmation dialogs, etc.) use an **item creator** strategy. Implement `IFlexItemCreator` and pass it to `SetItemsSource`. FlexList will call it and automatically refresh on success.

```csharp
public sealed class MenuItemCreator : IFlexItemCreator
{
    private readonly Dictionary<string, Func<MyItem>> _options;
    private readonly IList<MyItem> _list;

    public MenuItemCreator(IDictionary<string, Func<MyItem>> options, IList<MyItem> list)
    {
        _options = new Dictionary<string, Func<MyItem>>(options);
        _list = list;
    }

    public void RequestCreate(Action<bool> onComplete)
    {
        var menu = new GenericMenu();
        foreach (var kvp in _options)
        {
            menu.AddItem(new GUIContent(kvp.Key), false, () =>
            {
                var created = kvp.Value.Invoke();
                if (created != null) _list.Add(created);
                onComplete(created != null);
            });
        }

        menu.AddSeparator(string.Empty);
        menu.AddItem(new GUIContent("Cancel"), false, () => onComplete(false));
        menu.ShowAsContext();
    }
}

// Wiring it up:
var creator = new MenuItemCreator(
    new Dictionary<string, Func<MyItem>>
    {
        { "Basic", () => new MyItem() },
        { "Specialized/Type A", () => new MyItemA() },
        { "Specialized/Type B", () => new MyItemB() },
    },
    myItemList
);

// Overload that accepts IFlexItemCreator
flexList.SetItemsSource(myItemList, creator, removeItem, renderItem);
```

### Updating item titles without full rebuild
If an item's display name can change externally, update just the headers:
```csharp
// Refresh only the foldout titles (no full UI rebuild)
flexList.RefreshItemNames();

// Or update a single item by reference
flexList.RefreshItemName(changedItem);
```

## Roadmap
- [ ] Display name search / filtering API (`ApplyFilter`, `_visibleItems`).
- [ ] Built‑in duplicate.