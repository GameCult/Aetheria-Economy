

==tatertots==
you arrive at the palace of the caliph of ko. it is...starchy.
    *[chat with majordomo]
    there is...a complication.   ->outofpepper
    +{brunch_state ? egg && potatoes && bacon && cooking_utensils && fork} it is time. ->cookitup
    +[snoop] ->nosyparker
    +[request an audience] ->speak
    
    =cookitup
    brunch time! victory has never tasted so sweet.->END
    
    =speak
    you present your case. the caliph calls it an attache, and you insist it is a briefcase. an impartial judge is called in. it's a whole thing. in return for a brief respite from ennui, the caliph gifts you a potato upon departure. 
    ~get(potatoes)
    ~hunger_state = will_eat_spacehorse
    ->tatertots
    
    
    =outofpepper
        oh no. the kitchen are out of magic pepper.
        *{brunch_state ? pepper} [give them yours.]
        a great thank you. in return, the majordomo offers you the use of the kitchen, when you have completed your great ingredient quest. 
        ~get(kitchen_usage)
        ->tatertots
        *[hell no, that's your pepper. You <i>communed</i> for it.]
        mutterings. you leave with the steel-melting stare of a thwarted chef on your back. ->tatertots
    
    =nosyparker
    you snoop, as is your custom in a stranger's home. perhaps you pocket a few things that are not technically yours, who is to say? the finders keepers law has a strict nondisclosure clause.
    -(top)
        *[examine study]->study
        *[examine bedroom]->bedroom
        *[prowl the hallways like a very suspect panther]->hallways
        
        ==study
        it is a palatial study. you go through the desk drawers. it's not as interesting as media would suggest. ->tatertots.nosyparker.top
        
        ==bedroom
        the caliph is apparently very into velour. you recall that his ancestor was the legendary zapp brannigan, space captain and known velour fanatic. you munch on the chocolate drops in the nightstand drawer. 
        ~hunger_state--
        ->tatertots.nosyparker.top
        
        ==hallways
        you are quickly nabbed and removed from the premises.->END