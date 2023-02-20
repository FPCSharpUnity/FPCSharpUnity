namespace FPCSharpUnity.unity.Collection {
  public partial class Deque<T> {
    public ref T GetRef(int index) {
      checkIndexOutOfRange(index);
      return ref buffer[toBufferIndex(index)];
    }

    public bool backValueOut(out T value) {
      if (IsEmpty) {
        value = default;
        return false;
      }
      else {
        value = RemoveBack();
        return true;
      }
    }
    
    public bool frontValueOut(out T value) {
      if (IsEmpty) {
        value = default;
        return false;
      }
      else {
        value = RemoveFront();
        return true;
      }
    }
  }
}