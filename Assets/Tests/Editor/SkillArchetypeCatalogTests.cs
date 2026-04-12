using NUnit.Framework;

public class SkillArchetypeCatalogTests
{
    [Test]
    public void ResolveLegacyIcon_MapsExpectedIcons()
    {
        Assert.AreEqual(SkillArchetypeCatalog.Logic, SkillArchetypeCatalog.ResolveArchetypeIdFromLegacyIcon("DEV"));
        Assert.AreEqual(SkillArchetypeCatalog.Learning, SkillArchetypeCatalog.ResolveArchetypeIdFromLegacyIcon("BKS"));
        Assert.AreEqual(SkillArchetypeCatalog.General, SkillArchetypeCatalog.ResolveArchetypeIdFromLegacyIcon("???"));
    }

    [Test]
    public void GetDefinition_ReturnsCanonicalIconAndRussianName()
    {
        SkillArchetypeDefinition definition = SkillArchetypeCatalog.GetDefinition(SkillArchetypeCatalog.Culinary);

        Assert.NotNull(definition);
        Assert.AreEqual("CUL", definition.CanonicalIcon);
        Assert.AreEqual("Кулинария и дом", definition.DisplayName);
        Assert.AreEqual(SkillArchetypeCategory.Practical, definition.Category);
    }
}
