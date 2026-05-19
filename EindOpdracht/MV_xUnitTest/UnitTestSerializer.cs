using MV_BL.Helpers;
using Xunit;

namespace MV_xUnitTest;

/// <summary>
/// Tests for AnswerOrderSerializer.
/// Boundary analysis:
///   Serialize:   empty list → ""  ;  n-element list → comma-separated string
///   Deserialize: null / empty → empty list
///                whitespace around ints → trimmed correctly
///                roundtrip: Deserialize(Serialize(x)) == x
/// </summary>
public class UnitTestSerializer
{
	// Serialize

	[Fact]
	public void Test_Serialize_EmptyList_Valid()
	{
		Assert.Equal("", AnswerOrderSerializer.Serialize(new List<int>()));
	}

	[Theory]
	[InlineData(new[] { 0 },          "0")]
	[InlineData(new[] { 0, 1, 2, 3 }, "0,1,2,3")]
	[InlineData(new[] { 3, 1, 0, 2 }, "3,1,0,2")]
	public void Test_Serialize_Valid(int[] input, string expected)
	{
		Assert.Equal(expected, AnswerOrderSerializer.Serialize(input.ToList()));
	}

	// Deserialize

	[Fact]
	public void Test_Deserialize_Null_Valid()
	{
		Assert.Empty(AnswerOrderSerializer.Deserialize(null));
	}

	[Fact]
	public void Test_Deserialize_EmptyString_Valid()
	{
		Assert.Empty(AnswerOrderSerializer.Deserialize(""));
	}

	[Fact]
	public void Test_Deserialize_WhitespacePadded_Valid()
	{
		var result = AnswerOrderSerializer.Deserialize(" 1 , 2 , 3 ");
		Assert.Equal(new List<int> { 1, 2, 3 }, result);
	}

	// Roundtrip

	[Theory]
	[InlineData(new[] { 2, 0, 3, 1 })]
	[InlineData(new[] { 0, 1, 2, 3 })]
	[InlineData(new[] { 3, 2, 1, 0 })]
	public void Test_Serialize_Roundtrip_Valid(int[] input)
	{
		var list   = input.ToList();
		var csv    = AnswerOrderSerializer.Serialize(list);
		var result = AnswerOrderSerializer.Deserialize(csv);

		Assert.Equal(list, result);
	}
}
