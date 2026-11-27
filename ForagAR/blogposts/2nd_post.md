The second lab session was of shorter length.

First order of business was everyone showcasing their completed tasks assigned during the previous week; good progress was made there. Secondly, the group discussed what the next steps should be as our knowledge of how an AR application should be constructed was limited. We came to the conclusion, as there might not be further sessions where we get to learn about this, we should still try to make the most of the labs and create something based on our current knowledge about Unity. Salomeea and Alex took up the programming part, with Alex working on a basic UI for the application based on previous session’s sketched shared in our group chat and Salomeea would be adding further models into the application. 
There were some technical discoveries and considerations that needed to be made while importing the first model into the application. 

These were:
- The models vary heavily in size. They may be incredibly big, or incredibly small and thus their size after importing has to be adjusted accordingly. The first model, for example, was extremely big and its size had to be reduced a lot. It was scaled down on all axes to 0.03 of their original size.
- The place and orientation of the models can also differ. For example, the puffball mushroom is upside down, so when making sure that the mushroom wasn’t sinking halfway into the ground, along which axes to adjust the position of the model had to be considered as well and constant testing was important to ensure correctness.
- Importing the file type provided by the website (this being of the extension .gltf) that was previously found requires extra packages to be installed. (This was done, and unless complications occur, should pose no further issue.)
- The models also had to be adjusted a slight bit. The one that was first added, the giant puffball mushroom, had a tiny cube next to it in its model which was separate but served no purpose in our application, and thus was deleted along a few other assets to ensure the model would only have the bare necessities and would not get way too big. This is in consideration to an AR application’s limited capacity. Since the project is meant for phones, even if modern phones can handle a lot more, the less the project takes up in space, the smoother the experience will be for any end users.
- Rigidbodies should be added to the mushrooms with gravity disabled and the object set to kinematic. (This is based on observations from the sample scene.)

The sample scene’s UI was used to test the current, and only, imported mushroom model. And it was not deleted, only disabled. This was done as a guide, in case at any point in the future there might be something that we could use as a reference from the scene built, but it would not hinder our progress as it is disabled and we could build our own application next to it.

Further tasks involved more research into the mushrooms which Angela took and writing the next blog post which was taken by Kriszti.

Author: Kriszti

