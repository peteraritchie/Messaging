# Messaging

![chat](https://img.shields.io/gitter/room/peteraritchie/Messaging)

![Checks](https://img.shields.io/github/checks-status/peteraritchie/Messaging/main) 
![Total Downloads](https://img.shields.io/github/downloads/peteraritchie/Messaging/total)
![Downloads(latest)](https://img.shields.io/github/downloads/peteraritchie/Messaging/latest/total)
![contributors](https://img.shields.io/github/contributors/peteraritchie/Messaging)
![language](https://img.shields.io/github/languages/count/peteraritchie/Messaging)
![top-language](https://img.shields.io/github/languages/top/peteraritchie/Messaging)
![file-count](https://img.shields.io/github/directory-file-count/peteraritchie/Messaging)

## Packages

Package|Version|Description
-|-|-
`Pri.Messaging.Primitives`|[![NuGet version](https://badge.fury.io/nu/PRI.Messaging.Primitives.svg)](https://badge.fury.io/nu/PRI.Messaging.Primitives)|A small, independent and decoupled, class library to contain message-oriented abstractions. For the most part this library will only contain interfaces but may contain base classes in the future
`Pri.Messaging.Patterns`|[![NuGet version](https://badge.fury.io/nu/PRI.Messaging.Patterns.svg)](https://badge.fury.io/nu/PRI.Messaging.Patterns)|A library that contains patterns for implementing message-oriented systems.  The patterns are implementations from [`Pri.Messaging.Primitives`](https://www.nuget.org/packages/PRI.Messaging.Primitives).

## `Pri.Messaging.Primitives` Namespace

### `IMessage` Interface

Mostly a [marker interface](https://en.wikipedia.org/wiki/Marker_interface_pattern), but does contain a string `CorrelationId` property.  This interface provides a [message](http://www.enterpriseintegrationpatterns.com/patterns/messaging/Message.html) abstraction for other interfaces/implementations.

### `IEvent` Interface

A [marker interface](https://en.wikipedia.org/wiki/Marker_interface_pattern) to provide an event abstraction.  An event is a type of message; but a unique [event](http://www.enterpriseintegrationpatterns.com/patterns/messaging/EventMessage.html) abstraction allows messages and events to be handled separately.  An event is the representation of a immutable fact that describes something that occurred in the past.

### `ICommand` Interface

A [marker interface](https://en.wikipedia.org/wiki/Marker_interface_pattern) that provides an abstraction of a [command message](http://www.enterpriseintegrationpatterns.com/patterns/messaging/CommandMessage.html) specific to requesting a change in state, i.e. the execution of a command.

### `IConsumer` Interface

A generic interface that provides an abstraction for something that consumes or handles a message.   `IConsumer` has a `Handle` method to consume a messuage derived from `IMessage` and has the signature `void Handle<T>(T message)`

### `IProducer` Interface

A generic interface that provides an abstraction for something that produces a message.  `IProducer` has a `AttachConsumer` method to attach something that implements a `IConsumer` so that the producer and send the messages that are produced to the consumer.  This interface promotes the idea that messages are asynchronous and aren't singular.  The `AttachConsumer` method allows consumers to consume any number of messages that the producer will produce in the future.

### `IPipe` Interface

A convenient marker interface for something that is both a consumer and a producer, a [pipe](http://www.enterpriseintegrationpatterns.com/patterns/messaging/PipesAndFilters.html) (or filter).  It implements both `IConsumer` and `IProducer` and able to consume a message of one type and produce a method of another type.

### `IBus` Interface

A abstraction for a [bus](http://www.enterpriseintegrationpatterns.com/patterns/messaging/MessageBus.html) that when implemented would facilitate a decoupled architecture whose responsibility is to facilitate the connection of producers and consumers.

For a more in-depth introduction to Primitives, please see <http://blog.peterritchie.com/Introduction-to-messaging-primitives/>

## `Pri.Messaging.Patterns` Namespace

### `Bus` Class

A Bus is an simple implementation of [`IBus`](https://github.com/peteraritchie/Messaging/blob/main/Primitives/IBus.cs).  This class currently facilitates chaining message handlers or or consumers (implementations of [`IConsumer`](https://github.com/peteraritchie/Messaging/blob/main/Primitives/IConsumer.cs).

This bus provides the ability to automatically find and chain together handlers by providing a directory, wildcard and namespace specifier with the [`AddHandlersAndTranslators`](https://github.com/peteraritchie/Messaging/blob/main/Patterns/Extensions/Bus/BusExtensions.cs#L91) extension method.

A handler is an IConsumer implementation and a translator is an IPipe implementation and IPipes are also consumers.  As pipes are encountered they are connected to consumers of the pipes outgoing type.  So, when the bus is given a message to handle, the message is broadcast to all consumers; much like a [publish-subscribe channel](http://www.enterpriseintegrationpatterns.com/patterns/messaging/PublishSubscribeChannel.html).  If a consumer is a pipe, the pipe processes the message then sends it to another consumer.  If there is only one consumer of the message type to be handled by the bus, it will not broadcast but send to the one and only handler; like a [point-to-point channel](http://www.enterpriseintegrationpatterns.com/patterns/messaging/PointToPointChannel.html).

## `ActionConsumer<TMessage>` Interface

From [`Pri.Messaging.Primitives`](https://www.nuget.org/packages/PRI.Messaging.Primitives), `IConsumer<TMessage>` provides an interface to implement and pass-around message handlers.  But sometimes creating a new type to implement `IConsumer</TMessage>` may not make any sense.  `ActionConsumer<TMessage>` is an  implementation that lets you pass in a delegate or anonymous method that will handle the message.  For example, if you had a `MoveClientCommand` message that you needed to handle, you could add a handler to a bus like this:

```C#
    bus.AddHandler(new ActionConsumer<MoveClientCommand>(message => {
        var client = clientRepository.Get(message.ClientId);
        client.ChangeAddress(message.NewAddress);
    }));
```

### `ActionPipe<TMessageIn, TMessageOut>` Class

Along the same vane as `ActionConsumer<TMessage>`, from [`Pri.Messaging.Primitives`](https://www.nuget.org/packages/PRI.Messaging.Primitives), `IPipe<TMessageIn, TMessageOut>` provides an interface to implement and pass around a message translator or pipe.  Sometimes creating a new type to implement `IPipe<TMessageIn, TMessageOut>` is not the right thing to do.  `ActionPipe<TMessageIn, TMessageOut>` provides an `IPipe<TMessageIn, TMessageOut>` implementation where a translation method or anonymous method can be provided to perform the translation.  For example:

```C#
    bus.AddTranslator(new ActionPipe<MoveClientCommand,
        ChangeClientAddressCommand>(m=>new ChangeClientAddressCommand
            {
                CorrelationId = m.CorrelationId,
                NewAddress = m.NewAddress
            }
        ));
```
