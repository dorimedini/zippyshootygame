/**
 * <   > TileBehaviour.cs:178 is hitting null reference errors. It's the following line:
 *          distinctVerts[meshVertexMap[i]] = v[i];
 *       It comes with the infamous "Registered collision on tile XXX but collided list not initialized!" error I've been getting where I have no idea why.
 *       Is instance data being garbage collected during the lifecycle of a GameObject?
 *       Maybe it's about time I found out exactly when Start() and Awake() are called, and if they have any friends...
 */
