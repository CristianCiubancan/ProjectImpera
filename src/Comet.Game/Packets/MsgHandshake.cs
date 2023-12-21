// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgHandshake.cs
// Description:
// 
// Creator: FELIPEVIEIRAVENDRAMI [FELIPE VIEIRA VENDRAMINI]
// 
// Developed by:
// Felipe Vieira Vendramini <felipevendramini@live.com>
// 
// Programming today is a race between software engineers striving to build bigger and better
// idiot-proof programs, and the Universe trying to produce bigger and better idiots.
// So far, the Universe is winning.
// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#region References

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Comet.Game.States;
using Comet.Network.Packets;
using Comet.Network.Security;
using Org.BouncyCastle.Utilities.Encoders;

#endregion

namespace Comet.Game.Packets
{
    /// <summary>
    /// Message containing keys necessary for conducting the Diffie-Hellman key exchange.
    /// The initial message to the client is sent on connect, and contains initial seeds
    /// for Blowfish. The response message from the client then contains the shared key. 
    /// </summary>
    public sealed class MsgHandshake : MsgBase<Client>
    {
        /// <summary>
        /// Instantiates a new instance of <see cref="MsgHandshake"/>. This constructor
        /// is called to accept the client response.
        /// </summary>
        public MsgHandshake()
        {
        }

        /// <summary>
        /// Instantiates a new instance of <see cref="MsgHandshake"/>. This constructor
        /// is called to construct the initial request to the client.
        /// </summary>
        /// <param name="dh">Diffie-Hellman key exchange instance for the actor</param>
        /// <param name="encryptionIV">Initial seed for Blowfish's encryption IV</param>
        /// <param name="decryptionIV">Initial seed for Blowfish's decryption IV</param>
        public MsgHandshake(DiffieHellman dh, byte[] encryptionIV, byte[] decryptionIV)
        {
            PrimeRoot = Hex.ToHexString(dh.PrimeRoot.ToByteArrayUnsigned()).ToUpper();
            Generator = Hex.ToHexString(dh.Generator.ToByteArrayUnsigned()).ToUpper();
            ServerKey = Hex.ToHexString(dh.PublicKey.ToByteArrayUnsigned()).ToUpper();
            EncryptionIV = (byte[]) encryptionIV.Clone();
            DecryptionIV = (byte[]) decryptionIV.Clone();
        }

        // Packet Properties
        public byte[] DecryptionIV { get; private set; }
        public byte[] EncryptionIV { get; private set; }
        public string PrimeRoot { get; private set; }
        public string Generator { get; private set; }
        public string ServerKey { get; private set; }
        public string ClientKey { get; private set; }
        private byte[] Padding { get; set; }

        /// <summary>Randomizes padding for the message.</summary>
        public async Task RandomizeAsync()
        {
            Padding = new byte[23];
            await Kernel.NextBytesAsync(Padding);
        }

        /// <summary>
        /// Decodes a byte packet into the packet structure defined by this message class. 
        /// Should be invoked to structure data from the client for processing. Decoding
        /// follows TQ Digital's byte ordering rules for an all-binary protocol.
        /// </summary>
        /// <param name="bytes">Bytes from the packet processor or client socket</param>
        public override void Decode(byte[] bytes)
        {
            var reader = new PacketReader(bytes);
            reader.BaseStream.Seek(7, SeekOrigin.Begin);
            Length = (ushort) reader.ReadUInt32();
            reader.BaseStream.Seek(reader.ReadUInt32(), SeekOrigin.Current);
            ClientKey = Encoding.ASCII.GetString(reader.ReadBytes(reader.ReadInt32()));
        }

        /// <summary>
        /// Encodes the packet structure defined by this message class into a byte packet
        /// that can be sent to the client. Invoked automatically by the client's send 
        /// method. Encodes using byte ordering rules interoperable with the game client.
        /// </summary>
        /// <returns>Returns a byte packet of the encoded packet.</returns>
        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            var messageLength = 36 + Padding.Length + EncryptionIV.Length
                                + DecryptionIV.Length + PrimeRoot.Length + Generator.Length
                                + ServerKey.Length;
            
            // The packet writer class reserves 2 bytes for the send method to fill in for
            // the packet length. This message is an outlier to this pattern; however, 
            // leaving the reserved bytes does not affect the body of the message, so it
            // can be left in.

            writer.Write(Padding.AsSpan(0, 9));
            writer.Write(messageLength - 11);
            writer.Write(Padding.Length - 11);
            writer.Write(Padding.AsSpan(9, Padding.Length - 11));
            writer.Write(EncryptionIV.Length);
            writer.Write(EncryptionIV);
            writer.Write(DecryptionIV.Length);
            writer.Write(DecryptionIV);
            writer.Write(PrimeRoot.Length);
            writer.Write(PrimeRoot, PrimeRoot.Length);
            writer.Write(Generator.Length);
            writer.Write(Generator, Generator.Length);
            writer.Write(ServerKey.Length);
            writer.Write(ServerKey, ServerKey.Length);
            return writer.ToArray();
        }
    }
}