==A1==
#location: TestA // Inject this knot into matching knot in location story
#required // This quest is only playable when the target location exists
 + [This choice is being injected from TestQuest] -> A1A
 
 ==A1A==
 This is a thread being injected by TestQuest
 * [Proceed] -> A1B
 
==A1B==
This is a child knot in an injected thread
 * [Exit the injected thread] -> END