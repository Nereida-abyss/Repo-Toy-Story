using NUnit.Framework;
using UnityEngine;

public class PlayerMovementDashTests
{
    private GameObject playerObject;
    private Rigidbody playerRigidbody;
    private MovementScript movementScript;

    [SetUp]
    public void SetUp()
    {
        playerObject = new GameObject("PlayerDashTest");
        playerRigidbody = playerObject.AddComponent<Rigidbody>();
        playerRigidbody.useGravity = false;
        movementScript = playerObject.AddComponent<MovementScript>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(playerObject);
    }

    [Test]
    public void ResolveDashWorldDirection_UsesForward_WhenThereIsNoMoveInput()
    {
        playerObject.transform.rotation = Quaternion.Euler(0f, 45f, 0f);

        Vector3 dashDirection = movementScript.ResolveDashWorldDirection(Vector2.zero);

        Assert.That(Vector3.Angle(dashDirection, playerObject.transform.forward), Is.LessThan(0.1f));
    }

    [Test]
    public void ResolveDashWorldDirection_UsesRelativeMoveInput()
    {
        playerObject.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

        Vector3 dashDirection = movementScript.ResolveDashWorldDirection(new Vector2(1f, 0f));

        Assert.That(Vector3.Angle(dashDirection, playerObject.transform.right), Is.LessThan(0.1f));
    }

    [Test]
    public void TryStartDash_StartsCooldown_AndPreventsImmediateSecondDash()
    {
        bool firstDashStarted = movementScript.TryStartDash(Vector2.up);
        bool secondDashStarted = movementScript.TryStartDash(Vector2.up);

        Assert.That(firstDashStarted, Is.True);
        Assert.That(movementScript.IsDashing, Is.True);
        Assert.That(secondDashStarted, Is.False);
    }

    [Test]
    public void TryStartDash_PreservesVerticalVelocity()
    {
        playerRigidbody.linearVelocity = new Vector3(2f, 7f, 1f);

        bool dashStarted = movementScript.TryStartDash(Vector2.up);

        Assert.That(dashStarted, Is.True);
        Assert.That(playerRigidbody.linearVelocity.y, Is.EqualTo(7f).Within(0.001f));
    }

    [Test]
    public void TryStartDash_ConsumesAirDash_AndRechargesOnGroundContact()
    {
        SetPrivateField("isGrounded", false);

        bool firstAirDashStarted = movementScript.TryStartDash(Vector2.up);
        SetPrivateField("isDashing", false);
        SetPrivateField("dashCooldownTimer", 0f);
        bool secondAirDashStarted = movementScript.TryStartDash(Vector2.up);

        Assert.That(firstAirDashStarted, Is.True);
        Assert.That(secondAirDashStarted, Is.False);

        movementScript.SetExternalMovementState(Vector3.zero, true);
        movementScript.ClearExternalMovementState();
        SetPrivateField("dashCooldownTimer", 0f);

        bool groundedDashStarted = movementScript.TryStartDash(Vector2.up);
        Assert.That(groundedDashStarted, Is.True);
    }

    private void SetPrivateField(string fieldName, object value)
    {
        var field = typeof(MovementScript).GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"No se encontro el campo privado '{fieldName}'.");
        field.SetValue(movementScript, value);
    }

}
