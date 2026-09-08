namespace CrispySearchbar.Core.Configuration;

/// <summary>一组当前生效的快捷键映射；按键匹配与展示文案都从这里取。</summary>
public sealed class ShortcutCatalog
{
    private readonly IReadOnlyDictionary<ShortcutAction, ShortcutBinding> _bindings;
    private readonly IReadOnlyDictionary<ShortcutBinding, ShortcutAction> _actionsByBinding;

    private ShortcutCatalog(IReadOnlyDictionary<ShortcutAction, ShortcutBinding> bindings)
    {
        _bindings = bindings;
        _actionsByBinding = bindings.ToDictionary(
            pair => pair.Value,
            pair => pair.Key);
    }

    public static ShortcutCatalog Default { get; } = Create(new AppSettings());

    /// <summary>解析当前设置；单个字段无法解析时回退该操作的默认键。</summary>
    public static ShortcutCatalog Create(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var bindings = new Dictionary<ShortcutAction, ShortcutBinding>();
        foreach (var action in Enum.GetValues<ShortcutAction>())
        {
            var raw = ShortcutDefaults.GetValue(settings, action);
            if (ShortcutParser.IsEmpty(raw))
            {
                continue;
            }

            bindings[action] = ShortcutParser.TryParse(raw, out var binding)
                ? binding
                : ShortcutParser.TryParse(
                    ShortcutDefaults.GetDefaultValue(action),
                    out var fallback)
                    ? fallback
                    : throw new InvalidOperationException(
                        $"默认快捷键无效：{ShortcutDefaults.GetDefaultValue(action)}");
        }

        return new ShortcutCatalog(bindings);
    }

    public ShortcutBinding this[ShortcutAction action] => _bindings[action];

    public bool IsBound(ShortcutAction action) => _bindings.ContainsKey(action);

    public bool TryGetBinding(ShortcutAction action, out ShortcutBinding binding)
        => _bindings.TryGetValue(action, out binding!);

    public ShortcutAction? FindAction(ShortcutBinding pressed)
        => _actionsByBinding.TryGetValue(pressed, out var action)
            ? action
            : null;

    public string GetDisplay(ShortcutAction action)
        => _bindings[action].ToDisplayString();

    public string GetStorage(ShortcutAction action)
        => _bindings[action].ToStorageString();
}