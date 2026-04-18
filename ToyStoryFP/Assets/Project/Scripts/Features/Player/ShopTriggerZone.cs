using UnityEngine;

[DisallowMultipleComponent]
public class ShopTriggerZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        PlayerShopController controller = other.GetComponentInParent<PlayerShopController>();

        if (controller == null)
        {
            return;
        }

        controller.SetInsideShopZone(true);
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerShopController controller = other.GetComponentInParent<PlayerShopController>();

        if (controller == null)
        {
            return;
        }

        controller.SetInsideShopZone(false);
    }
}
