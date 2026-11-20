using Domain.RoomTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BusinessLogicTest;

[TestClass]
public sealed class RoomTypeFactoryTests
{
    [TestMethod]
    public void TryGet_ReturnsFalse_WhenKeyIsNull()
    {
        bool result = RoomTypeFactory.TryGet(null!, out RoomType? type);

        Assert.IsFalse(result);
        Assert.IsNull(type);
    }

    [TestMethod]
    public void TryGet_ReturnsFalse_WhenKeyIsEmpty()
    {
        bool result = RoomTypeFactory.TryGet(string.Empty, out RoomType? type);

        Assert.IsFalse(result);
        Assert.IsNull(type);
    }

    [TestMethod]
    public void TryGet_ReturnsFalse_WhenKeyIsWhiteSpace()
    {
        bool result = RoomTypeFactory.TryGet("   ", out RoomType? type);

        Assert.IsFalse(result);
        Assert.IsNull(type);
    }

    [TestMethod]
    public void TryGet_ReturnsTrue_WhenKeyHasDifferentCasingAndWhitespace()
    {
        bool result = RoomTypeFactory.TryGet("  sala  ", out RoomType? type);

        Assert.IsTrue(result);
        Assert.AreEqual(SalaRoomType.Instance, type);
    }
}
