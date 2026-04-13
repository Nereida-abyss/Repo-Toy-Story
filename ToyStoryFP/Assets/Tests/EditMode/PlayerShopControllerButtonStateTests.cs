using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class PlayerShopControllerButtonStateTests
{
    [Test]
    public void UpdateButtonStates_KeepsUnaffordableUpgradesClickable_WithDimmedVisuals()
    {
        GameObject playerObject = new GameObject("PlayerShopControllerTest");
        PlayerShopController controller = playerObject.AddComponent<PlayerShopController>();
        PlayerCurrencyController currencyController = playerObject.AddComponent<PlayerCurrencyController>();

        Button speedButton = CreateButton("SpeedButton");
        Button jumpButton = CreateButton("JumpButton");
        Button ammoButton = CreateButton("AmmoButton");
        Button healButton = CreateButton("HealButton");
        Button m16Button = CreateButton("M16Button");
        Button akButton = CreateButton("AkButton");

        try
        {
            SetPrivateField(controller, "currencyController", currencyController);
            SetPrivateField(controller, "speedButton", speedButton);
            SetPrivateField(controller, "jumpButton", jumpButton);
            SetPrivateField(controller, "ammoButton", ammoButton);
            SetPrivateField(controller, "healButton", healButton);
            SetPrivateField(controller, "m16Button", m16Button);
            SetPrivateField(controller, "akButton", akButton);

            InvokePrivateMethod(controller, "UpdateButtonStates");

            Assert.That(speedButton.interactable, Is.True);
            Assert.That(jumpButton.interactable, Is.True);
            Assert.That(speedButton.GetComponent<CanvasGroup>(), Is.Not.Null);
            Assert.That(jumpButton.GetComponent<CanvasGroup>(), Is.Not.Null);
            Assert.That(speedButton.GetComponent<CanvasGroup>().alpha, Is.EqualTo(0.6f).Within(0.001f));
            Assert.That(jumpButton.GetComponent<CanvasGroup>().alpha, Is.EqualTo(0.6f).Within(0.001f));

            Assert.That(ammoButton.interactable, Is.False);
            Assert.That(healButton.interactable, Is.False);
            Assert.That(m16Button.interactable, Is.False);
            Assert.That(akButton.interactable, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(speedButton.gameObject);
            Object.DestroyImmediate(jumpButton.gameObject);
            Object.DestroyImmediate(ammoButton.gameObject);
            Object.DestroyImmediate(healButton.gameObject);
            Object.DestroyImmediate(m16Button.gameObject);
            Object.DestroyImmediate(akButton.gameObject);
            Object.DestroyImmediate(playerObject);
        }
    }

    private static Button CreateButton(string name)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.AddComponent<RectTransform>();
        buttonObject.AddComponent<CanvasRenderer>();
        buttonObject.AddComponent<Image>();
        return buttonObject.AddComponent<Button>();
    }

    private static void InvokePrivateMethod(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, $"No se encontro el metodo privado '{methodName}'.");
        method.Invoke(target, null);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"No se encontro el campo privado '{fieldName}'.");
        field.SetValue(target, value);
    }
}
