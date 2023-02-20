namespace FPCSharpUnity.unity.Collection {
  public partial class Deque<T> {
    public ref T GetRef(int index) {
      checkIndexOutOfRange(index);
      return ref buffer[toBufferIndex(index)];
    }

    public bool removeBackValue(out T value) {
      if (IsEmpty) {
        value = default;
        return false;
      }
      else {
        value = RemoveBack();
        return true;
      }
    }
    
    public bool removeFrontValue(out T value) {
      if (IsEmpty) {
        value = default;
        return false;
      }
      else {
        value = RemoveFront();
        return true;
      }
    }
    
    public bool peekBack(out T value) {
      if (IsEmpty) {
        value = default;
        return false;
      }
      else {
        value = Get(Count - 1);
        return true;
      }
    }
    
    public bool peekFront(out T value) {
      if (IsEmpty) {
        value = default;
        return false;
      }
      else {
        value = Get(0);
        return true;
      }
    }
  }
}