using NUnit.Framework;
using UnityEngine;

public class UiIconProviderTests
{
    [Test]
    public void GetSprite_ReturnsGeneratedSpriteForKnownIconId()
    {
        Sprite sprite = UiIconProvider.GetSprite("DEV");

        Assert.NotNull(sprite);
        Assert.NotNull(sprite.texture);
        Assert.Greater(sprite.texture.width, 0);
        Assert.Greater(sprite.texture.height, 0);
    }

    [Test]
    public void GetSprite_ReusesCachedGeneratedSprite()
    {
        Sprite first = UiIconProvider.GetSprite("SNACK");
        Sprite second = UiIconProvider.GetSprite("SNACK");

        Assert.NotNull(first);
        Assert.AreSame(first, second);
    }
}
