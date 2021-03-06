/* This code is autogenerated from tuples.cs */
using System;
using System.Collections.Immutable;
using FPCSharpUnity.core.data;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;

namespace FPCSharpUnity.unity.Functional {
  
  public static class EitherValidationExts {
  

public static Either<ImmutableList<A>, B> asValidation<A, B>(
  this Either<A, B> e1
) { return e1.mapLeft(ImmutableList.Create); }
      

public static Either<ImmutableList<A>, Tpl<P1, P2>> validateAnd<A, P1, P2>(
  this Either<A, P1> e1, Either<A, P2> e2
) {
  
  foreach (var b in e1.rightValue)
    foreach (var c in e2.rightValue)
      return Either<ImmutableList<A>, Tpl<P1, P2>>.Right(Tpl.a(b, c));

  var arr = ImmutableList<A>.Empty;
  foreach (var a in e1.leftValue) arr = arr.Add(a);
  foreach (var a in e2.leftValue) arr = arr.Add(a);
  return Either<ImmutableList<A>, Tpl<P1, P2>>.Left(arr);
}

public static Either<ImmutableList<A>, Tpl<P1, P2>> and<A, P1, P2>(
  this Either<ImmutableList<A>, P1> e1, Either<A, P2> e2
) {
  
  foreach (var b in e1.rightValue)
    foreach (var c in e2.rightValue)
      return Either<ImmutableList<A>, Tpl<P1, P2>>.Right(Tpl.a(b, c));

  
  var arr = e1.leftValue.fold(ImmutableList<A>.Empty, _ => _);
  foreach (var a in e2.leftValue) arr = arr.Add(a);
  return Either<ImmutableList<A>, Tpl<P1, P2>>.Left(arr);

}

public static Either<ImmutableList<A>, Tpl<P1, P2>> and<A, P1, P2>(
  this Either<ImmutableList<A>, P1> e1, Either<ImmutableList<A>, P2> e2
) {
  
  foreach (var b in e1.rightValue)
    foreach (var c in e2.rightValue)
      return Either<ImmutableList<A>, Tpl<P1, P2>>.Right(Tpl.a(b, c));

  
  var arr = e1.leftValue.fold(ImmutableList<A>.Empty, _ => _);
  foreach (var arr2 in e2.leftValue)
    foreach (var a in arr2) arr = arr.Add(a);
  return Either<ImmutableList<A>, Tpl<P1, P2>>.Left(arr);

}
     

public static Either<ImmutableList<A>, Tpl<P1, P2, P3>> and<A, P1, P2, P3>(
  this Either<ImmutableList<A>, Tpl<P1, P2>> e1,
  Either<A, P3> e2
) {
  
  foreach (var tpl in e1.rightValue)
    foreach (var value in e2.rightValue)
      return Either<ImmutableList<A>, Tpl<P1, P2, P3>>.Right(tpl.add(value));

  
  var arr = e1.leftValue.fold(ImmutableList<A>.Empty, _ => _);
  foreach (var a in e2.leftValue) arr = arr.Add(a);
  return Either<ImmutableList<A>, Tpl<P1, P2, P3>>.Left(arr);

}

public static Either<ImmutableList<A>, Tpl<P1, P2, P3>> and<A, P1, P2, P3>(
  this Either<ImmutableList<A>, Tpl<P1, P2>> e1,
  Either<ImmutableList<A>, P3> e2
) {
  
  foreach (var tpl in e1.rightValue)
    foreach (var value in e2.rightValue)
      return Either<ImmutableList<A>, Tpl<P1, P2, P3>>.Right(tpl.add(value));

  
  var arr = e1.leftValue.fold(ImmutableList<A>.Empty, _ => _);
  foreach (var arr2 in e2.leftValue)
    foreach (var a in arr2) arr = arr.Add(a);
  return Either<ImmutableList<A>, Tpl<P1, P2, P3>>.Left(arr);

}
       

public static Either<ImmutableList<A>, Tpl<P1, P2, P3, P4>> and<A, P1, P2, P3, P4>(
  this Either<ImmutableList<A>, Tpl<P1, P2, P3>> e1,
  Either<A, P4> e2
) {
  
  foreach (var tpl in e1.rightValue)
    foreach (var value in e2.rightValue)
      return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4>>.Right(tpl.add(value));

  
  var arr = e1.leftValue.fold(ImmutableList<A>.Empty, _ => _);
  foreach (var a in e2.leftValue) arr = arr.Add(a);
  return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4>>.Left(arr);

}

public static Either<ImmutableList<A>, Tpl<P1, P2, P3, P4>> and<A, P1, P2, P3, P4>(
  this Either<ImmutableList<A>, Tpl<P1, P2, P3>> e1,
  Either<ImmutableList<A>, P4> e2
) {
  
  foreach (var tpl in e1.rightValue)
    foreach (var value in e2.rightValue)
      return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4>>.Right(tpl.add(value));

  
  var arr = e1.leftValue.fold(ImmutableList<A>.Empty, _ => _);
  foreach (var arr2 in e2.leftValue)
    foreach (var a in arr2) arr = arr.Add(a);
  return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4>>.Left(arr);

}
       

public static Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5>> and<A, P1, P2, P3, P4, P5>(
  this Either<ImmutableList<A>, Tpl<P1, P2, P3, P4>> e1,
  Either<A, P5> e2
) {
  
  foreach (var tpl in e1.rightValue)
    foreach (var value in e2.rightValue)
      return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5>>.Right(tpl.add(value));

  
  var arr = e1.leftValue.fold(ImmutableList<A>.Empty, _ => _);
  foreach (var a in e2.leftValue) arr = arr.Add(a);
  return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5>>.Left(arr);

}

public static Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5>> and<A, P1, P2, P3, P4, P5>(
  this Either<ImmutableList<A>, Tpl<P1, P2, P3, P4>> e1,
  Either<ImmutableList<A>, P5> e2
) {
  
  foreach (var tpl in e1.rightValue)
    foreach (var value in e2.rightValue)
      return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5>>.Right(tpl.add(value));

  
  var arr = e1.leftValue.fold(ImmutableList<A>.Empty, _ => _);
  foreach (var arr2 in e2.leftValue)
    foreach (var a in arr2) arr = arr.Add(a);
  return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5>>.Left(arr);

}
       

public static Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6>> and<A, P1, P2, P3, P4, P5, P6>(
  this Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5>> e1,
  Either<A, P6> e2
) {
  
  foreach (var tpl in e1.rightValue)
    foreach (var value in e2.rightValue)
      return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6>>.Right(tpl.add(value));

  
  var arr = e1.leftValue.fold(ImmutableList<A>.Empty, _ => _);
  foreach (var a in e2.leftValue) arr = arr.Add(a);
  return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6>>.Left(arr);

}

public static Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6>> and<A, P1, P2, P3, P4, P5, P6>(
  this Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5>> e1,
  Either<ImmutableList<A>, P6> e2
) {
  
  foreach (var tpl in e1.rightValue)
    foreach (var value in e2.rightValue)
      return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6>>.Right(tpl.add(value));

  
  var arr = e1.leftValue.fold(ImmutableList<A>.Empty, _ => _);
  foreach (var arr2 in e2.leftValue)
    foreach (var a in arr2) arr = arr.Add(a);
  return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6>>.Left(arr);

}
       

public static Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7>> and<A, P1, P2, P3, P4, P5, P6, P7>(
  this Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6>> e1,
  Either<A, P7> e2
) {
  
  foreach (var tpl in e1.rightValue)
    foreach (var value in e2.rightValue)
      return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7>>.Right(tpl.add(value));

  
  var arr = e1.leftValue.fold(ImmutableList<A>.Empty, _ => _);
  foreach (var a in e2.leftValue) arr = arr.Add(a);
  return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7>>.Left(arr);

}

public static Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7>> and<A, P1, P2, P3, P4, P5, P6, P7>(
  this Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6>> e1,
  Either<ImmutableList<A>, P7> e2
) {
  
  foreach (var tpl in e1.rightValue)
    foreach (var value in e2.rightValue)
      return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7>>.Right(tpl.add(value));

  
  var arr = e1.leftValue.fold(ImmutableList<A>.Empty, _ => _);
  foreach (var arr2 in e2.leftValue)
    foreach (var a in arr2) arr = arr.Add(a);
  return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7>>.Left(arr);

}
       

public static Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8>> and<A, P1, P2, P3, P4, P5, P6, P7, P8>(
  this Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7>> e1,
  Either<A, P8> e2
) {
  
  foreach (var tpl in e1.rightValue)
    foreach (var value in e2.rightValue)
      return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8>>.Right(tpl.add(value));

  
  var arr = e1.leftValue.fold(ImmutableList<A>.Empty, _ => _);
  foreach (var a in e2.leftValue) arr = arr.Add(a);
  return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8>>.Left(arr);

}

public static Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8>> and<A, P1, P2, P3, P4, P5, P6, P7, P8>(
  this Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7>> e1,
  Either<ImmutableList<A>, P8> e2
) {
  
  foreach (var tpl in e1.rightValue)
    foreach (var value in e2.rightValue)
      return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8>>.Right(tpl.add(value));

  
  var arr = e1.leftValue.fold(ImmutableList<A>.Empty, _ => _);
  foreach (var arr2 in e2.leftValue)
    foreach (var a in arr2) arr = arr.Add(a);
  return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8>>.Left(arr);

}
       

public static Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9>> and<A, P1, P2, P3, P4, P5, P6, P7, P8, P9>(
  this Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8>> e1,
  Either<A, P9> e2
) {
  
  foreach (var tpl in e1.rightValue)
    foreach (var value in e2.rightValue)
      return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9>>.Right(tpl.add(value));

  
  var arr = e1.leftValue.fold(ImmutableList<A>.Empty, _ => _);
  foreach (var a in e2.leftValue) arr = arr.Add(a);
  return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9>>.Left(arr);

}

public static Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9>> and<A, P1, P2, P3, P4, P5, P6, P7, P8, P9>(
  this Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8>> e1,
  Either<ImmutableList<A>, P9> e2
) {
  
  foreach (var tpl in e1.rightValue)
    foreach (var value in e2.rightValue)
      return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9>>.Right(tpl.add(value));

  
  var arr = e1.leftValue.fold(ImmutableList<A>.Empty, _ => _);
  foreach (var arr2 in e2.leftValue)
    foreach (var a in arr2) arr = arr.Add(a);
  return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9>>.Left(arr);

}
       

public static Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10>> and<A, P1, P2, P3, P4, P5, P6, P7, P8, P9, P10>(
  this Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9>> e1,
  Either<A, P10> e2
) {
  
  foreach (var tpl in e1.rightValue)
    foreach (var value in e2.rightValue)
      return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10>>.Right(tpl.add(value));

  
  var arr = e1.leftValue.fold(ImmutableList<A>.Empty, _ => _);
  foreach (var a in e2.leftValue) arr = arr.Add(a);
  return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10>>.Left(arr);

}

public static Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10>> and<A, P1, P2, P3, P4, P5, P6, P7, P8, P9, P10>(
  this Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9>> e1,
  Either<ImmutableList<A>, P10> e2
) {
  
  foreach (var tpl in e1.rightValue)
    foreach (var value in e2.rightValue)
      return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10>>.Right(tpl.add(value));

  
  var arr = e1.leftValue.fold(ImmutableList<A>.Empty, _ => _);
  foreach (var arr2 in e2.leftValue)
    foreach (var a in arr2) arr = arr.Add(a);
  return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10>>.Left(arr);

}
       

public static Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11>> and<A, P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11>(
  this Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10>> e1,
  Either<A, P11> e2
) {
  
  foreach (var tpl in e1.rightValue)
    foreach (var value in e2.rightValue)
      return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11>>.Right(tpl.add(value));

  
  var arr = e1.leftValue.fold(ImmutableList<A>.Empty, _ => _);
  foreach (var a in e2.leftValue) arr = arr.Add(a);
  return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11>>.Left(arr);

}

public static Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11>> and<A, P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11>(
  this Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10>> e1,
  Either<ImmutableList<A>, P11> e2
) {
  
  foreach (var tpl in e1.rightValue)
    foreach (var value in e2.rightValue)
      return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11>>.Right(tpl.add(value));

  
  var arr = e1.leftValue.fold(ImmutableList<A>.Empty, _ => _);
  foreach (var arr2 in e2.leftValue)
    foreach (var a in arr2) arr = arr.Add(a);
  return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11>>.Left(arr);

}
       

public static Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12>> and<A, P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12>(
  this Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11>> e1,
  Either<A, P12> e2
) {
  
  foreach (var tpl in e1.rightValue)
    foreach (var value in e2.rightValue)
      return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12>>.Right(tpl.add(value));

  
  var arr = e1.leftValue.fold(ImmutableList<A>.Empty, _ => _);
  foreach (var a in e2.leftValue) arr = arr.Add(a);
  return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12>>.Left(arr);

}

public static Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12>> and<A, P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12>(
  this Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11>> e1,
  Either<ImmutableList<A>, P12> e2
) {
  
  foreach (var tpl in e1.rightValue)
    foreach (var value in e2.rightValue)
      return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12>>.Right(tpl.add(value));

  
  var arr = e1.leftValue.fold(ImmutableList<A>.Empty, _ => _);
  foreach (var arr2 in e2.leftValue)
    foreach (var a in arr2) arr = arr.Add(a);
  return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12>>.Left(arr);

}
       

public static Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13>> and<A, P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13>(
  this Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12>> e1,
  Either<A, P13> e2
) {
  
  foreach (var tpl in e1.rightValue)
    foreach (var value in e2.rightValue)
      return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13>>.Right(tpl.add(value));

  
  var arr = e1.leftValue.fold(ImmutableList<A>.Empty, _ => _);
  foreach (var a in e2.leftValue) arr = arr.Add(a);
  return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13>>.Left(arr);

}

public static Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13>> and<A, P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13>(
  this Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12>> e1,
  Either<ImmutableList<A>, P13> e2
) {
  
  foreach (var tpl in e1.rightValue)
    foreach (var value in e2.rightValue)
      return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13>>.Right(tpl.add(value));

  
  var arr = e1.leftValue.fold(ImmutableList<A>.Empty, _ => _);
  foreach (var arr2 in e2.leftValue)
    foreach (var a in arr2) arr = arr.Add(a);
  return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13>>.Left(arr);

}
       

public static Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14>> and<A, P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14>(
  this Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13>> e1,
  Either<A, P14> e2
) {
  
  foreach (var tpl in e1.rightValue)
    foreach (var value in e2.rightValue)
      return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14>>.Right(tpl.add(value));

  
  var arr = e1.leftValue.fold(ImmutableList<A>.Empty, _ => _);
  foreach (var a in e2.leftValue) arr = arr.Add(a);
  return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14>>.Left(arr);

}

public static Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14>> and<A, P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14>(
  this Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13>> e1,
  Either<ImmutableList<A>, P14> e2
) {
  
  foreach (var tpl in e1.rightValue)
    foreach (var value in e2.rightValue)
      return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14>>.Right(tpl.add(value));

  
  var arr = e1.leftValue.fold(ImmutableList<A>.Empty, _ => _);
  foreach (var arr2 in e2.leftValue)
    foreach (var a in arr2) arr = arr.Add(a);
  return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14>>.Left(arr);

}
       

public static Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15>> and<A, P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15>(
  this Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14>> e1,
  Either<A, P15> e2
) {
  
  foreach (var tpl in e1.rightValue)
    foreach (var value in e2.rightValue)
      return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15>>.Right(tpl.add(value));

  
  var arr = e1.leftValue.fold(ImmutableList<A>.Empty, _ => _);
  foreach (var a in e2.leftValue) arr = arr.Add(a);
  return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15>>.Left(arr);

}

public static Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15>> and<A, P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15>(
  this Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14>> e1,
  Either<ImmutableList<A>, P15> e2
) {
  
  foreach (var tpl in e1.rightValue)
    foreach (var value in e2.rightValue)
      return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15>>.Right(tpl.add(value));

  
  var arr = e1.leftValue.fold(ImmutableList<A>.Empty, _ => _);
  foreach (var arr2 in e2.leftValue)
    foreach (var a in arr2) arr = arr.Add(a);
  return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15>>.Left(arr);

}
       

public static Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16>> and<A, P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16>(
  this Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15>> e1,
  Either<A, P16> e2
) {
  
  foreach (var tpl in e1.rightValue)
    foreach (var value in e2.rightValue)
      return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16>>.Right(tpl.add(value));

  
  var arr = e1.leftValue.fold(ImmutableList<A>.Empty, _ => _);
  foreach (var a in e2.leftValue) arr = arr.Add(a);
  return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16>>.Left(arr);

}

public static Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16>> and<A, P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16>(
  this Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15>> e1,
  Either<ImmutableList<A>, P16> e2
) {
  
  foreach (var tpl in e1.rightValue)
    foreach (var value in e2.rightValue)
      return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16>>.Right(tpl.add(value));

  
  var arr = e1.leftValue.fold(ImmutableList<A>.Empty, _ => _);
  foreach (var arr2 in e2.leftValue)
    foreach (var a in arr2) arr = arr.Add(a);
  return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16>>.Left(arr);

}
       

public static Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17>> and<A, P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17>(
  this Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16>> e1,
  Either<A, P17> e2
) {
  
  foreach (var tpl in e1.rightValue)
    foreach (var value in e2.rightValue)
      return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17>>.Right(tpl.add(value));

  
  var arr = e1.leftValue.fold(ImmutableList<A>.Empty, _ => _);
  foreach (var a in e2.leftValue) arr = arr.Add(a);
  return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17>>.Left(arr);

}

public static Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17>> and<A, P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17>(
  this Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16>> e1,
  Either<ImmutableList<A>, P17> e2
) {
  
  foreach (var tpl in e1.rightValue)
    foreach (var value in e2.rightValue)
      return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17>>.Right(tpl.add(value));

  
  var arr = e1.leftValue.fold(ImmutableList<A>.Empty, _ => _);
  foreach (var arr2 in e2.leftValue)
    foreach (var a in arr2) arr = arr.Add(a);
  return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17>>.Left(arr);

}
       

public static Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17, P18>> and<A, P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17, P18>(
  this Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17>> e1,
  Either<A, P18> e2
) {
  
  foreach (var tpl in e1.rightValue)
    foreach (var value in e2.rightValue)
      return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17, P18>>.Right(tpl.add(value));

  
  var arr = e1.leftValue.fold(ImmutableList<A>.Empty, _ => _);
  foreach (var a in e2.leftValue) arr = arr.Add(a);
  return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17, P18>>.Left(arr);

}

public static Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17, P18>> and<A, P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17, P18>(
  this Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17>> e1,
  Either<ImmutableList<A>, P18> e2
) {
  
  foreach (var tpl in e1.rightValue)
    foreach (var value in e2.rightValue)
      return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17, P18>>.Right(tpl.add(value));

  
  var arr = e1.leftValue.fold(ImmutableList<A>.Empty, _ => _);
  foreach (var arr2 in e2.leftValue)
    foreach (var a in arr2) arr = arr.Add(a);
  return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17, P18>>.Left(arr);

}
       

public static Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17, P18, P19>> and<A, P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17, P18, P19>(
  this Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17, P18>> e1,
  Either<A, P19> e2
) {
  
  foreach (var tpl in e1.rightValue)
    foreach (var value in e2.rightValue)
      return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17, P18, P19>>.Right(tpl.add(value));

  
  var arr = e1.leftValue.fold(ImmutableList<A>.Empty, _ => _);
  foreach (var a in e2.leftValue) arr = arr.Add(a);
  return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17, P18, P19>>.Left(arr);

}

public static Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17, P18, P19>> and<A, P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17, P18, P19>(
  this Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17, P18>> e1,
  Either<ImmutableList<A>, P19> e2
) {
  
  foreach (var tpl in e1.rightValue)
    foreach (var value in e2.rightValue)
      return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17, P18, P19>>.Right(tpl.add(value));

  
  var arr = e1.leftValue.fold(ImmutableList<A>.Empty, _ => _);
  foreach (var arr2 in e2.leftValue)
    foreach (var a in arr2) arr = arr.Add(a);
  return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17, P18, P19>>.Left(arr);

}
       

public static Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17, P18, P19, P20>> and<A, P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17, P18, P19, P20>(
  this Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17, P18, P19>> e1,
  Either<A, P20> e2
) {
  
  foreach (var tpl in e1.rightValue)
    foreach (var value in e2.rightValue)
      return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17, P18, P19, P20>>.Right(tpl.add(value));

  
  var arr = e1.leftValue.fold(ImmutableList<A>.Empty, _ => _);
  foreach (var a in e2.leftValue) arr = arr.Add(a);
  return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17, P18, P19, P20>>.Left(arr);

}

public static Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17, P18, P19, P20>> and<A, P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17, P18, P19, P20>(
  this Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17, P18, P19>> e1,
  Either<ImmutableList<A>, P20> e2
) {
  
  foreach (var tpl in e1.rightValue)
    foreach (var value in e2.rightValue)
      return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17, P18, P19, P20>>.Right(tpl.add(value));

  
  var arr = e1.leftValue.fold(ImmutableList<A>.Empty, _ => _);
  foreach (var arr2 in e2.leftValue)
    foreach (var a in arr2) arr = arr.Add(a);
  return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17, P18, P19, P20>>.Left(arr);

}
       

public static Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17, P18, P19, P20, P21>> and<A, P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17, P18, P19, P20, P21>(
  this Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17, P18, P19, P20>> e1,
  Either<A, P21> e2
) {
  
  foreach (var tpl in e1.rightValue)
    foreach (var value in e2.rightValue)
      return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17, P18, P19, P20, P21>>.Right(tpl.add(value));

  
  var arr = e1.leftValue.fold(ImmutableList<A>.Empty, _ => _);
  foreach (var a in e2.leftValue) arr = arr.Add(a);
  return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17, P18, P19, P20, P21>>.Left(arr);

}

public static Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17, P18, P19, P20, P21>> and<A, P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17, P18, P19, P20, P21>(
  this Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17, P18, P19, P20>> e1,
  Either<ImmutableList<A>, P21> e2
) {
  
  foreach (var tpl in e1.rightValue)
    foreach (var value in e2.rightValue)
      return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17, P18, P19, P20, P21>>.Right(tpl.add(value));

  
  var arr = e1.leftValue.fold(ImmutableList<A>.Empty, _ => _);
  foreach (var arr2 in e2.leftValue)
    foreach (var a in arr2) arr = arr.Add(a);
  return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17, P18, P19, P20, P21>>.Left(arr);

}
       

public static Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17, P18, P19, P20, P21, P22>> and<A, P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17, P18, P19, P20, P21, P22>(
  this Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17, P18, P19, P20, P21>> e1,
  Either<A, P22> e2
) {
  
  foreach (var tpl in e1.rightValue)
    foreach (var value in e2.rightValue)
      return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17, P18, P19, P20, P21, P22>>.Right(tpl.add(value));

  
  var arr = e1.leftValue.fold(ImmutableList<A>.Empty, _ => _);
  foreach (var a in e2.leftValue) arr = arr.Add(a);
  return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17, P18, P19, P20, P21, P22>>.Left(arr);

}

public static Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17, P18, P19, P20, P21, P22>> and<A, P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17, P18, P19, P20, P21, P22>(
  this Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17, P18, P19, P20, P21>> e1,
  Either<ImmutableList<A>, P22> e2
) {
  
  foreach (var tpl in e1.rightValue)
    foreach (var value in e2.rightValue)
      return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17, P18, P19, P20, P21, P22>>.Right(tpl.add(value));

  
  var arr = e1.leftValue.fold(ImmutableList<A>.Empty, _ => _);
  foreach (var arr2 in e2.leftValue)
    foreach (var a in arr2) arr = arr.Add(a);
  return Either<ImmutableList<A>, Tpl<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15, P16, P17, P18, P19, P20, P21, P22>>.Left(arr);

}
       
  }
}
  
