namespace DocWriter
{
	public abstract class TemplateBase<T>
	{
		public T Model { get; set; }

		public abstract string GenerateString();
	}
}
