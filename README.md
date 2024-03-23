# SceneExplorer

**_Cities: Skylines II_** modification that lets you explore various internal data of selected in-game objects such as ECS component data.
When simulation is running the data is automatically updated.
Since game entities may be composed from many components, framerate drop is expected during live-refresh of such large sets of data (will be improved in the future)

#### In-Game or Editor features
* Use **Ctrl+E** to open _Scene Explorer Inspector_ window
* Use **Ctrl+W** to open _Scene Explorer Component Search_ window
* Open _Entity Query Search_ window to find entities based on configurable query

#### Editor-only features
* When _Entity Query_ search window shows at least one entity listed, use **Ctrl+S** to make a snapshot
* Open _Snapshots_ window with a button from _Component Search_ window to see/clear snapshotted entities
