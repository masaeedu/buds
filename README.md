# buds
Peer to peer communication via an Rx interface to ZeroMQ messaging

The goal of this library is to facilitate simple service bus communication between peers without having the consumer explicitly specify a broker. 

Currently the implementation uses naive UDP broadcast for discovery and each node maintains a list of observed peers. The only reliable strategy for dealing with message delivery failures is via timeout.
