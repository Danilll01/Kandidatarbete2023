mergeInto(LibraryManager.library, {
  // Tells the page that the game is ready to quit, through react-unity-webgl's event system when it is used
  ReadyToQuit: function () {
    if (typeof window.dispatchReactUnityEvent === "function") window.dispatchReactUnityEvent("ReadyToQuit");
  },
});
