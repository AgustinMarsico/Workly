using Domain.RoomTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BusinessLogicTest;

[TestClass]
public sealed class RoomTypeTests
{
    [TestMethod]
    public void Equals_ReturnsTrue_WhenKeysAreEqual()
    {
        RoomType left = SalaRoomType.Instance;
        RoomType right = new TestRoomType(SalaRoomType.Instance.Key, "Test Sala");

        Assert.IsTrue(left.Equals(right));
        Assert.IsTrue(left.Equals((object)right));
        Assert.IsTrue(left == right);
        Assert.IsFalse(left != right);
    }

    [TestMethod]
    public void Equals_ReturnsFalse_WhenOtherIsNull()
    {
        RoomType roomType = SalaRoomType.Instance;

        Assert.IsFalse(roomType.Equals((RoomType?)null));
        Assert.IsFalse(roomType == null);
        Assert.IsTrue(roomType != null);
    }

    [TestMethod]
    public void Equals_ReturnsFalse_WhenOtherHasDifferentKey()
    {
        RoomType left = SalaRoomType.Instance;
        RoomType right = AuditorioRoomType.Instance;

        Assert.IsFalse(left.Equals(right));
        Assert.IsFalse(left == right);
        Assert.IsTrue(left != right);
    }

    [TestMethod]
    public void Equals_ReturnsFalse_WhenOtherIsDifferentType()
    {
        RoomType roomType = SalaRoomType.Instance;

        Assert.IsFalse(roomType.Equals("Sala"));
    }

    [TestMethod]
    public void EqualityOperator_ReturnsTrue_WhenBothSidesAreNull()
    {
        RoomType? left = null;
        RoomType? right = null;

        Assert.IsTrue(left == right);
        Assert.IsFalse(left != right);
    }

    [TestMethod]
    public void GetHashCode_UsesKey()
    {
        RoomType roomType = SalaRoomType.Instance;

        int result = roomType.GetHashCode();

        Assert.AreEqual(StringComparer.Ordinal.GetHashCode(roomType.Key), result);
    }

    private sealed class TestRoomType(string key, string displayName) : RoomType
    {
        public override string Key { get; } = key;

        public override string DisplayName { get; } = displayName;
    }
}
