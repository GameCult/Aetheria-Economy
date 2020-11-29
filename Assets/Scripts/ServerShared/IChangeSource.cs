using System;
using System.Collections;
using System.Collections.Generic;


public interface IChangeSource  
{  
    event Action OnChanged;  
}  
