namespace Domain.Common;

/// <summary>Valor de salida de un procedimiento, igual al patrón de las referencias.</summary>
public sealed class OutputParameter<TValue>
{
    private bool _valueSet;
    private TValue _value = default!;

    public TValue Value => _valueSet
        ? _value
        : throw new InvalidOperationException("El valor de salida no fue establecido.");

    public void SetValue(object? value)
    {
        _valueSet = true;
        _value = value is null or DBNull ? default! : (TValue)value;
    }
}
