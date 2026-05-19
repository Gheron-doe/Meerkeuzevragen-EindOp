namespace MV_BL.Exceptions;

public class UnsupportedFormatException : MeerkeuzevragenException
{
	public string Format { get; }
	public string FactoryName { get; }
	public UnsupportedFormatException(string format, string factoryName)
		: base($"Format '{format}' is not registered in {factoryName}.")
	{
		Format = format;
		FactoryName = factoryName;
	}
}
