using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PanelControllerCreditsImageBindingTests
{
    [Test]
    public void ShowCreditImageForEntry_KeepsExistingPlaceholderSprite_WhenBindingSpriteIsNull()
    {
        GameObject controllerObject = new GameObject("PanelControllerCreditsTest");
        GameObject imageObject = new GameObject("CreditImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        GameObject textObject = new GameObject("CreditText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        Texture2D initialTexture = new Texture2D(2, 2);
        Sprite initialSprite = Sprite.Create(initialTexture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f));

        try
        {
            PanelController controller = controllerObject.AddComponent<PanelController>();
            Image targetImage = imageObject.GetComponent<Image>();
            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = "Cristian Rivero Llacer (elHabana)";
            targetImage.sprite = initialSprite;
            targetImage.enabled = false;

            object binding = CreateCreditImageBinding(
                text.text,
                targetImage,
                null,
                preserveAspect: true,
                setNativeSize: false);

            IList bindings = CreateBindingList(binding);
            SetPrivateField(controller, "creditImageBindings", bindings);

            object entry = CreateCreditTextEntry(text);
            InvokePrivateMethod(controller, "ShowCreditImageForEntry", entry);

            Assert.That(targetImage.sprite, Is.SameAs(initialSprite));
            Assert.That(targetImage.enabled, Is.True);
            Assert.That(targetImage.preserveAspect, Is.True);
        }
        finally
        {
            if (initialSprite != null)
            {
                UnityEngine.Object.DestroyImmediate(initialSprite);
            }

            UnityEngine.Object.DestroyImmediate(initialTexture);
            UnityEngine.Object.DestroyImmediate(textObject);
            UnityEngine.Object.DestroyImmediate(imageObject);
            UnityEngine.Object.DestroyImmediate(controllerObject);
        }
    }

    private static object CreateCreditTextEntry(TMP_Text text)
    {
        Type entryType = GetNestedType("CreditTextEntry");
        object entry = Activator.CreateInstance(entryType, nonPublic: true);
        RectTransform rectTransform = text.rectTransform;

        SetField(entry, "Text", text);
        SetField(entry, "RectTransform", rectTransform);
        SetField(entry, "OriginalAnchoredPosition", rectTransform.anchoredPosition);
        SetField(entry, "OriginalColor", text.color);

        return entry;
    }

    private static object CreateCreditImageBinding(string matchText, Image targetImage, Sprite sprite, bool preserveAspect, bool setNativeSize)
    {
        Type bindingType = GetNestedType("CreditImageBinding");
        object binding = Activator.CreateInstance(bindingType, nonPublic: true);

        SetField(binding, "MatchText", matchText);
        SetField(binding, "TargetImage", targetImage);
        SetField(binding, "Sprite", sprite);
        SetField(binding, "PreserveAspect", preserveAspect);
        SetField(binding, "SetNativeSize", setNativeSize);

        return binding;
    }

    private static IList CreateBindingList(object binding)
    {
        Type bindingType = GetNestedType("CreditImageBinding");
        Type listType = typeof(System.Collections.Generic.List<>).MakeGenericType(bindingType);
        IList list = (IList)Activator.CreateInstance(listType);
        list.Add(binding);
        return list;
    }

    private static Type GetNestedType(string typeName)
    {
        Type type = typeof(PanelController).GetNestedType(typeName, BindingFlags.NonPublic);
        Assert.That(type, Is.Not.Null, $"No se encontro el tipo anidado privado '{typeName}'.");
        return type;
    }

    private static void InvokePrivateMethod(object target, string methodName, object argument)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, $"No se encontro el metodo privado '{methodName}'.");
        method.Invoke(target, new[] { argument });
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"No se encontro el campo privado '{fieldName}'.");
        field.SetValue(target, value);
    }

    private static void SetField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"No se encontro el campo '{fieldName}'.");
        field.SetValue(target, value);
    }
}
