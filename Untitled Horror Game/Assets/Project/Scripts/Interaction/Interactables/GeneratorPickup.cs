using System.Runtime.CompilerServices;
using UnityEngine;

public enum GeneratorPickUpType
{
    GeneratorPart, Fuel
}
public class GeneratorPickup : MonoBehaviour
{
    [SerializeField]private GeneratorPickUpType generatorPickUpType;

    [SerializeField]private Generator generatorTask;

    private bool destroyOnPickup = true;

    public void CollectPickUp()
    {
        if(generatorPickUpType == GeneratorPickUpType.GeneratorPart)
            generatorTask.AddGeneratorPart();

        else if (generatorPickUpType == GeneratorPickUpType.Fuel)
            generatorTask.AddFuel();

        Debug.Log(gameObject.name + "Collected");

        if (destroyOnPickup)
            Destroy(gameObject);

    }
}
