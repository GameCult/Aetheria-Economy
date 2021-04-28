->mmm



==mmm==
you land where the fearsome rasherbeast of glug dwells. 
-(options) you find yourself at a crossroads.
+[hunt the beast]->beast_hunting
+[talk to Sir Ogg]->Sir_Ogg
+[leave] ->END


=beast_hunting
you go immediately to hunt the beast. you are, after all, an apex predator.
the beast, alas, can travel through time as well as space. you must reset until you find a loop containing it.
~statedetermination()
{loopnum ? lots: 
you find the creature. ->beastie

-else:
no luck. ->options
}


    =beastie
    {hunger_state >= moderate:  
        you are weak from hunger. the beast kills you.
    -else:
    you harvest its flesh. it smells like cooking human. 
    ~get(bacon) 
    [bacon in inventory]
    ->options
    }

=Sir_Ogg
Sir Ogg is a knight, on a quest. You are also on a quest, so you bond immediately. Sir Ogg wishes to slay a dragon. unfortunately, there are no dragons anywhere in aetheria.
    *[help ogg]->quest
    *[laugh and leave] ->options
    
    =quest
    the problem, therefore, is how to slay a dragon with no dragons. the only solution seems to be for you to put on a scaled cloak and make dragon noises while Sir Ogg fake slays you with a stick. its good fun all around. ->options
    
    
    



