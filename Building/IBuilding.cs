using System.Collections.Generic;
using Unity.VisualScripting;
using  UnityEngine;

public interface IBuilding
{
   
    Yields Yields { get; }
    int SpecialistSlots { get; }
    List<string> Prerequisites { get; }
 
}