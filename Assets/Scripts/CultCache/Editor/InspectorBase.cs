public abstract class InspectorBase<T>
{
    protected const int width = 150;
    protected const int labelWidth = 20;
    protected const int toggleWidth = 18;
    protected const int arrowWidth = 22;
    
    public abstract T Inspect(string label, T value);
}