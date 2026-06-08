using Mirror;
using UnityEngine;

public enum GeneratorPickUpType
{
    GeneratorPart, Fuel
}
public class GeneratorPickup : NetworkBehaviour
{
    [SerializeField]private GeneratorPickUpType generatorPickUpType;

    [SerializeField]private Generator generatorTask;

    [SerializeField] private bool destroyOnPickup = true;

    [Server]
    public void CollectPickUp()
    {
        if (generatorPickUpType == GeneratorPickUpType.GeneratorPart)
            generatorTask.AddGeneratorPart();
        
        else if (generatorPickUpType == GeneratorPickUpType.Fuel)
            generatorTask.AddFuel();

        Debug.Log(gameObject.name + "Collected");

        if (destroyOnPickup)
            NetworkServer.Destroy(gameObject);

    }
}
