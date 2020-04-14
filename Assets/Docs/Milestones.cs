/**
 * < V > Stuff I already did
 * <   > Connect network layer, play with it active without terrible performance
 * ------- > Create room if can't random join, or join random room
 * ------- > On join, instantiate player on random pentagon
 * ------- > Disable main camera on join
 * ------- > Disable physics, camera, audiolistener on player prefab, enable on self join room
 * ------- > Find the equivalent of a PhotonView, make it watch a NetworkCharacter Monobehaviour
 * ------- > NetworkCharacter should do a 0.1f Lerp from current position/rotation to actual position/rotation while moving
 * <   > Add player model and animation (ready-made, temporary)
 * <   > Add projectile damage
 * <   > Successfully play with 2 people concurrently, have one take damage from the other
 * <   > Weapon model and animation (maybe make in blender? Or take ready made)
 * <   > Movement mechanic - grapple on pillar edge and launch
 * <   > Better projectile animation (how to launch with precision without shooting from the camera?)
 *?<   > Better projectile model...?
 * <   > Texture the projectile
 * <   > Particle effects for projectile in air and on hit
 * <   > Get non-temporary player models
 * <   > Non-temporary weapon models
 * <   > Pentagon hit should exand pentagon to the max, and any hex in an EHN-depth BFS from the pentagon should expand by min(maxheight, height+maxheight/(deg+1))
 * <   > Make new list
 */
