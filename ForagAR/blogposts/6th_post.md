This week, we added floating panels to ForagAR, worked out how to handle mushroom assets that were too large to push to GitHub, and started building the VR environment. We also put together the basic UI for the XR project so we have something to build on as the interactions take shape.
Adding floating panels
We added floating panels in Unity so that parts of the interface can appear within the scene. This gives us a way to place information near the object it relates to. For example, a panel could show a mushroom’s name and details next to its 3D model, so users can look at both while exploring.

Adding the panels also made us think about placement differently. In a regular menu, we can decide where something goes based on the edges of the screen. In a 3D scene, we also have to consider where the user is standing, how far away the panel is, and whether it covers the object they’re trying to see. Those are things we can adjust as we try the panels in the scene.

Some of the mushroom assets were too large to push as they were, so we spent time figuring out which files were taking up the most space and how to reduce their size. We looked at compressing the images used by the models, since the textures contribute to the overall size of the assets. The aim was to make the files smaller while keeping details such as the color and texture of the mushroom caps visible.

We also had to pay attention to what Git was picking up. Unity generates local files as it imports and processes assets, and those can make the change list much bigger than expected. When the list showed more than 32,000 changes, we had to look more closely at which files belonged in the commit. The plan was to put the optimized mushroom assets on a separate branch so the team could review them before they went into main.

We also began setting up the environment for the VR project in Unity. At this stage, the scene gives us a place to start arranging objects and thinking about how users will move through the space. Even with a basic layout, we can start asking practical questions about how close users should get to a mushroom, how large the models should appear, and where a floating panel would be easiest to read.

Having a scene to work in also gives us a better sense of how the assets and interface might work together. A mushroom model can look fine on its own in the Unity editor, but seeing it next to other objects in the environment helps us judge its scale and placement.

We put together the first version of the XR UI. We focused on getting the basic layout in place so we could see how the interface fits alongside the objects in the scene. As we add more interactions, we can use this version to decide which controls need to stay visible and which information should appear when the user interacts with something.

Author: Salomeea
