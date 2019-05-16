﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace benchmark.utils
{
    public sealed class ChaCha20
    {
        #region Constants
        private const string ALG_NAME = "ChaCha";
        private const int BLOCK_SIZE = 64;
        private const int CTR_SIZE = 8;
        private const int DEF_ROUNDS = 20;
        private const int MAXALLOC_MB100 = 100000000;
        private const int MAX_ROUNDS = 30;
        private const int MIN_ROUNDS = 8;
        private const int PARALLEL_CHUNK = 1024;
        private const int PRL_BLOCKCACHE = 32000;
        private const int STATE_SIZE = 16;
        private static readonly byte[] SIGMA = System.Text.Encoding.ASCII.GetBytes("expand 32-byte k");
        private static readonly byte[] TAU = System.Text.Encoding.ASCII.GetBytes("expand 16-byte k");
        #endregion

        #region Fields
        private uint[] m_ctrVector = new uint[2];
        private byte[] m_dstCode = null;
        private bool m_isDisposed = false;
        private bool m_isInitialized = false;
        private bool m_isParallel = false;
        private int m_parallelBlockSize = PRL_BLOCKCACHE;
        private int m_parallelMaxDegree = 0;
        private int m_parallelMinimumSize = 0;
        private ParallelOptions m_parallelOption = null;
        private int m_processorCount = 1;
        private int m_rndCount = DEF_ROUNDS;
        private uint[] m_wrkBuffer = new uint[STATE_SIZE];
        private uint[] m_wrkState = new uint[14];
        #endregion

        #region Properties
        /// <summary>
	    /// Get: Unit block size of internal cipher in bytes.
	    /// <para>Block size is 64 bytes wide.</para>
	    /// </summary>
        public int BlockSize { get { return BLOCK_SIZE; } }

        /// <summary>
        /// Get the current counter value
        /// </summary>
        public long Counter
        {
            get { return ((long) m_ctrVector[1] << 32) | (m_ctrVector[0] & 0xffffffffL); }
        }

        /// <summary>
        /// Get/Set: Sets the Nonce value in the initialization parameters (Tau-Sigma). 
        /// <para>Must be set before <see cref="Initialize(KeyParams)"/> is called.
        /// Changing this code will create a unique distribution of the cipher.
        /// Code must be 16 bytes in length and sufficiently asymmetric (no more than 2 repeats, of 2 bytes, at a distance of 2 intervals).</para>
        /// </summary>
        /// 
        /// <exception cref="CryptoSymmetricException">Thrown if an invalid distribution code is used</exception>
        public byte[] DistributionCode
        {
            get { return m_dstCode; }
            set
            {
               
                m_dstCode = value;
            }
        }
        /// <summary>
        /// Get: Cipher is ready to transform data
        /// </summary>
        public bool IsInitialized
        {
            get { return m_isInitialized; }
            private set { m_isInitialized = value; }
        }

        /// <summary>
        /// Get/Set: Automatic processor parallelization
        /// </summary>
        public bool IsParallel
        {
            get { return m_isParallel; }
            set { m_isParallel = value; }
        }

        /// <summary>
        /// Get: Available Encryption Key Sizes in bytes
        /// </summary>
        public int[] LegalKeySizes
        {
            get { return new int[] { 16, 32 }; }
        }

        /// <summary>
        /// Get: Available diffusion round assignments
        /// </summary>
        public int[] LegalRounds
        {
            get { return new int[] { 8, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30 }; }
        }

        /// <summary>
        /// Get: Cipher name
        /// </summary>
        public string Name
        {
            get { return ALG_NAME; }
        }

        /// <summary>
        /// Get/Set: Parallel block size. Must be a multiple of <see cref="ParallelMinimumSize"/>.
        /// </summary>
        /// 
        /// <exception cref="CryptoSymmetricException">Thrown if a parallel block size is not evenly divisible by ParallelMinimumSize, or  block size is less than ParallelMinimumSize or more than ParallelMaximumSize values</exception>
        public int ParallelBlockSize
        {
            get { return m_parallelBlockSize; }
            set
            {
                if (value % ParallelMinimumSize != 0)
                    throw new Exception(String.Format("Parallel block size must be evenly divisible by ParallelMinimumSize: {0}", ParallelMinimumSize), new ArgumentException());
                if (value > ParallelMaximumSize || value < ParallelMinimumSize)
                    throw new Exception(String.Format("Parallel block must be Maximum of ParallelMaximumSize: {0} and evenly divisible by ParallelMinimumSize: {1}", ParallelMaximumSize, ParallelMinimumSize), new ArgumentOutOfRangeException());

                m_parallelBlockSize = value;
            }
        }

        /// <summary>
        /// Get: Maximum input size with parallel processing
        /// </summary>
        public int ParallelMaximumSize
        {
            get { return MAXALLOC_MB100; }
        }

        /// <summary>
        /// Get: The smallest parallel block size. Parallel blocks must be a multiple of this size.
        /// </summary>
        public int ParallelMinimumSize
        {
            get { return m_parallelMinimumSize; }
        }

        /// <summary>
        /// Get/Set: The parallel loops ParallelOptions
        /// <para>The MaxDegreeOfParallelism of the parallel loop is equal to the Environment.ProcessorCount by default</para>
        /// </summary>
        public ParallelOptions ParallelOption
        {
            get
            {
                if (m_parallelOption == null)
                    m_parallelOption = new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount };

                return m_parallelOption;
            }
            set
            {
                if (value != null)
                {
                    if (value.MaxDegreeOfParallelism < 1)
                        throw new Exception("ChaCha:ParallelOption"+ "MaxDegreeOfParallelism can not be less than 1!", new ArgumentException());
                    else if (value.MaxDegreeOfParallelism == 1)
                        m_isParallel = false;
                    else if (value.MaxDegreeOfParallelism % 2 != 0)
                        throw new Exception("ChaCha:ParallelOption" + "MaxDegreeOfParallelism can not be an odd number; must be either 1, or a divisible of 2!", new ArgumentException());

                    m_parallelOption = value;
                }
            }
        }

        /// <remarks>
        /// Get: Processor count
        /// </remarks>
        private int ProcessorCount
        {
            get { return m_processorCount; }
            set { m_processorCount = value; }
        }

        /// <summary>
        /// Get: Number of rounds
        /// </summary>
        public int Rounds
        {
            get { return m_rndCount; }
            private set { m_rndCount = value; }
        }

        /// <summary>
        /// Get: Initialization vector size
        /// </summary>
        public int VectorSize
        {
            get { return CTR_SIZE; }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Initialize the class
        /// </summary>
        /// 
        /// <param name="Rounds">Number of diffusion rounds. The <see cref="LegalRounds"/> property contains available sizes. Default is 20 rounds.</param>
        /// 
        /// <exception cref="CryptoSymmetricException">Thrown if an invalid rounds count is chosen</exception>
        public ChaCha20(int Rounds = DEF_ROUNDS)
        {
            if (Rounds <= 0 || (Rounds & 1) != 0)
                throw new Exception("ChaCha:Ctor"+ "Rounds must be a positive even number!", new ArgumentOutOfRangeException());
            if (Rounds < MIN_ROUNDS || Rounds > MAX_ROUNDS)
                throw new Exception("ChaCha:Ctor"+ String.Format("Rounds must be between {0} and {1)!", MIN_ROUNDS, MAX_ROUNDS), new ArgumentOutOfRangeException());

            m_rndCount = Rounds;
            Scope();
        }

        /// <summary>
        /// Finalize objects
        /// </summary>
        ~ChaCha20()
        {
            Dispose(false);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the Cipher
        /// </summary>
        /// 
        /// <param name="KeyParam">Cipher key container. 
        /// <para>Uses the Key and IV fields of KeyParam. 
        /// The <see cref="LegalKeySizes"/> property contains valid Key sizes. 
        /// IV must be 8 bytes in size.</para>
        /// </param>
        /// 
        /// <exception cref="System.ArgumentNullException">Thrown if a null key or iv  is used</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown if an invalid key or iv size  is used</exception>
        public void Initialize(KeyParams KeyParam)
        {
            // recheck params
            Scope();

            if (KeyParam.IV == null || KeyParam.IV.Length != 8)
                throw new Exception("ChaCha20:Initialize"+ "Init parameters must include an IV!", new ArgumentException());
            if (KeyParam.Key == null || KeyParam.Key.Length != 16 && KeyParam.Key.Length != 32)
                throw new Exception("ChaCha20:Initialize"+ "Key must be 16 or 32 bytes!", new ArgumentException());
            if (IsParallel && ParallelBlockSize < ParallelMinimumSize || ParallelBlockSize > ParallelMaximumSize)
                throw new Exception("ChaCha20:Initialize"+"The parallel block size is out of bounds!");
            if (IsParallel && ParallelBlockSize % ParallelMinimumSize != 0)
                throw new Exception("ChaCha20:Initialize"+ "The parallel block size must be evenly aligned to the ParallelMinimumSize!");

            if (DistributionCode == null)
            {
                if (KeyParam.Key.Length == 16)
                    m_dstCode = (byte[]) TAU.Clone();
                else
                    m_dstCode = (byte[]) SIGMA.Clone();
            }

            Reset();
            SetKey(KeyParam.Key, KeyParam.IV);

            m_isInitialized = true;
        }

        /// <summary>
        /// Reset the primary internal counter
        /// </summary>
        public void Reset()
        {
            m_ctrVector[0] = m_ctrVector[1] = 0;
        }

        /// <summary>
        /// Process an array of bytes. 
        /// <para>This method processes the entire array; used when processing small data or buffers from a larger source.
        /// Parallel capable function if Input array length is at least equal to <see cref="ParallelBlockSize"/>. 
        /// <see cref="Initialize(KeyParams)"/> must be called before this method can be used.</para>
        /// </summary>
        /// 
        /// <param name="Input">Bytes to Transform</param>
        /// <param name="Output">Transformed bytes</param>
        public void Transform(byte[] Input, byte[] Output)
        {
            Process(Input, 0, Output, 0, Input.Length);
        }

        /// <summary>
        /// Process a block of bytes using offset parameters.  
        /// <para>
        /// This method will process a single block from the source array of either ParallelBlockSize or Blocksize depending on IsParallel property setting.
        /// Parallel capable function if Input array length is at least equal to <see cref="ParallelBlockSize"/>. 
        /// Partial blocks are permitted with both parallel and linear operation modes.
        /// The <see cref="Initialize(KeyParams)"/> method must be called before this method can be used.</para>
        /// </summary>
        /// 
        /// <param name="Input">Bytes to Transform</param>
        /// <param name="InOffset">Offset in the Input array</param>
        /// <param name="Output">Transformed bytes</param>
        /// <param name="OutOffset">Offset in the Output array</param>
        public void Transform(byte[] Input, int InOffset, byte[] Output, int OutOffset)
        {
            Process(Input, InOffset, Output, OutOffset, m_isParallel ? m_parallelBlockSize : BLOCK_SIZE);
        }

        /// <summary>
        /// Process an array of bytes with offset and length parameters.
        /// <para>This method processes a specified length of the array; used when processing segments of a large source array.
        /// Parallel capable function if Length is at least equal to <see cref="ParallelBlockSize"/>.
        /// This method automatically assigns the ParallelBlockSize as the Length divided by the number of processors.
        /// <see cref="Initialize(KeyParams)"/> must be called before this method can be used.</para>
        /// </summary>
        /// 
        /// <param name="Input">Bytes to Transform</param>
        /// <param name="InOffset">Offset in the Input array</param>
        /// <param name="Output">Transformed bytes</param>
        /// <param name="OutOffset">Offset in the Output array</param>
        /// <param name="Length">Number of bytes to process</param>
        public void Transform(byte[] Input, int InOffset, byte[] Output, int OutOffset, int Length)
        {
            Process(Input, InOffset, Output, OutOffset, Length);
        }
        #endregion

        #region Key Schedule
        private void SetKey(byte[] Key, byte[] Iv)
        {
            if (Key != null)
            {
                if (Key.Length == 32)
                {
                    m_wrkState[0] = IntUtils.BytesToLe32(m_dstCode, 0);
                    m_wrkState[1] = IntUtils.BytesToLe32(m_dstCode, 4);
                    m_wrkState[2] = IntUtils.BytesToLe32(m_dstCode, 8);
                    m_wrkState[3] = IntUtils.BytesToLe32(m_dstCode, 12);
                    m_wrkState[4] = IntUtils.BytesToLe32(Key, 0);
                    m_wrkState[5] = IntUtils.BytesToLe32(Key, 4);
                    m_wrkState[6] = IntUtils.BytesToLe32(Key, 8);
                    m_wrkState[7] = IntUtils.BytesToLe32(Key, 12);
                    m_wrkState[8] = IntUtils.BytesToLe32(Key, 16);
                    m_wrkState[9] = IntUtils.BytesToLe32(Key, 20);
                    m_wrkState[10] = IntUtils.BytesToLe32(Key, 24);
                    m_wrkState[11] = IntUtils.BytesToLe32(Key, 28);
                    m_wrkState[12] = IntUtils.BytesToLe32(Iv, 0);
                    m_wrkState[13] = IntUtils.BytesToLe32(Iv, 4);

                }
                else
                {
                    m_wrkState[0] = IntUtils.BytesToLe32(m_dstCode, 0);
                    m_wrkState[1] = IntUtils.BytesToLe32(m_dstCode, 4);
                    m_wrkState[2] = IntUtils.BytesToLe32(m_dstCode, 8);
                    m_wrkState[3] = IntUtils.BytesToLe32(m_dstCode, 12);
                    m_wrkState[4] = IntUtils.BytesToLe32(Key, 0);
                    m_wrkState[5] = IntUtils.BytesToLe32(Key, 4);
                    m_wrkState[6] = IntUtils.BytesToLe32(Key, 8);
                    m_wrkState[7] = IntUtils.BytesToLe32(Key, 12);
                    m_wrkState[8] = IntUtils.BytesToLe32(Key, 0);
                    m_wrkState[9] = IntUtils.BytesToLe32(Key, 4);
                    m_wrkState[10] = IntUtils.BytesToLe32(Key, 8);
                    m_wrkState[11] = IntUtils.BytesToLe32(Key, 12);
                    m_wrkState[12] = IntUtils.BytesToLe32(Iv, 0);
                    m_wrkState[13] = IntUtils.BytesToLe32(Iv, 4);
                }
            }
        }
        #endregion

        #region Transform
        private void Generate(int Size, uint[] Counter, byte[] Output, int OutOffset)
        {
            int aln = Size - (Size % BLOCK_SIZE);
            int ctr = 0;

            while (ctr != aln)
            {
                Transform(Output, OutOffset + ctr, Counter);
                Increment(Counter);
                ctr += BLOCK_SIZE;
            }

            if (ctr != Size)
            {
                byte[] outputBlock = new byte[BLOCK_SIZE];
                Transform(outputBlock, 0, Counter);
                int fnlSize = Size % BLOCK_SIZE;
                Buffer.BlockCopy(outputBlock, 0, Output, OutOffset + (Size - fnlSize), fnlSize);
                Increment(Counter);
            }
        }

        private void Process(byte[] Input, int InOffset, byte[] Output, int OutOffset, int Length)
        {
            int prcSze = (Length >= Input.Length - InOffset) && Length >= Output.Length - OutOffset ? IntUtils.Min(Input.Length - InOffset, Output.Length - OutOffset) : Length;

            if (!m_isParallel || prcSze < m_parallelBlockSize)
            {
                // generate random
                Generate(prcSze, m_ctrVector, Output, OutOffset);
                // output is input xor with random
                int sze = prcSze - (prcSze % BLOCK_SIZE);

                if (sze != 0)
                    IntUtils.XORBLK(Input, InOffset, Output, OutOffset, sze);

                // get the remaining bytes
                if (sze != prcSze)
                {
                    for (int i = sze; i < prcSze; ++i)
                        Output[i + OutOffset] ^= Input[i + InOffset];
                }
            }
            else
            {
                // parallel CTR processing //
                int cnkSize = (prcSze / BLOCK_SIZE / ProcessorCount) * BLOCK_SIZE;
                int rndSize = cnkSize * ProcessorCount;
                int subSize = (cnkSize / BLOCK_SIZE);
                // create jagged array of 'sub counters'
                uint[] tmpCtr = new uint[m_ctrVector.Length];

                // create random, and xor to output in parallel
                System.Threading.Tasks.Parallel.For(0, m_processorCount, i =>
                {
                    // thread level counter
                    uint[] thdCtr = new uint[m_ctrVector.Length];
                    // offset counter by chunk size / block size
                    thdCtr = Increase(m_ctrVector, subSize * i);
                    // create random at offset position
                    this.Generate(cnkSize, thdCtr, Output, OutOffset + (i * cnkSize));
                    // xor with input at offset
                    IntUtils.XORBLK(Input, InOffset + (i * cnkSize), Output, OutOffset + (i * cnkSize), cnkSize);
                    // store last counter
                    if (i == m_processorCount - 1)
                        Array.Copy(thdCtr, 0, tmpCtr, 0, thdCtr.Length);
                });

                // last block processing
                if (rndSize < prcSze)
                {
                    int fnlSize = prcSze % rndSize;
                    Generate(fnlSize, tmpCtr, Output, rndSize);

                    for (int i = 0; i < fnlSize; ++i)
                        Output[i + OutOffset + rndSize] ^= (byte) (Input[i + InOffset + rndSize]);
                }

                // copy the last counter position to class variable
                Array.Copy(tmpCtr, 0, m_ctrVector, 0, m_ctrVector.Length);
            }
        }

        private void Transform(byte[] Output, int OutOffset, uint[] Counter)
        {
            int ctr = 0;
            uint X0 = m_wrkState[ctr];
            uint X1 = m_wrkState[++ctr];
            uint X2 = m_wrkState[++ctr];
            uint X3 = m_wrkState[++ctr];
            uint X4 = m_wrkState[++ctr];
            uint X5 = m_wrkState[++ctr];
            uint X6 = m_wrkState[++ctr];
            uint X7 = m_wrkState[++ctr];
            uint X8 = m_wrkState[++ctr];
            uint X9 = m_wrkState[++ctr];
            uint X10 = m_wrkState[++ctr];
            uint X11 = m_wrkState[++ctr];
            uint X12 = Counter[0];
            uint X13 = Counter[1];
            uint X14 = m_wrkState[++ctr];
            uint X15 = m_wrkState[++ctr];

            ctr = Rounds;
            while (ctr != 0)
            {
                X0 += X4;
                X12 = IntUtils.RotateLeft(X12 ^ X0, 16);
                X8 += X12;
                X4 = IntUtils.RotateLeft(X4 ^ X8, 12);
                X0 += X4;
                X12 = IntUtils.RotateLeft(X12 ^ X0, 8);
                X8 += X12;
                X4 = IntUtils.RotateLeft(X4 ^ X8, 7);
                X1 += X5;
                X13 = IntUtils.RotateLeft(X13 ^ X1, 16);
                X9 += X13;
                X5 = IntUtils.RotateLeft(X5 ^ X9, 12);
                X1 += X5;
                X13 = IntUtils.RotateLeft(X13 ^ X1, 8);
                X9 += X13;
                X5 = IntUtils.RotateLeft(X5 ^ X9, 7);
                X2 += X6;
                X14 = IntUtils.RotateLeft(X14 ^ X2, 16);
                X10 += X14;
                X6 = IntUtils.RotateLeft(X6 ^ X10, 12);
                X2 += X6;
                X14 = IntUtils.RotateLeft(X14 ^ X2, 8);
                X10 += X14;
                X6 = IntUtils.RotateLeft(X6 ^ X10, 7);
                X3 += X7;
                X15 = IntUtils.RotateLeft(X15 ^ X3, 16);
                X11 += X15;
                X7 = IntUtils.RotateLeft(X7 ^ X11, 12);
                X3 += X7;
                X15 = IntUtils.RotateLeft(X15 ^ X3, 8);
                X11 += X15;
                X7 = IntUtils.RotateLeft(X7 ^ X11, 7);
                X0 += X5;
                X15 = IntUtils.RotateLeft(X15 ^ X0, 16);
                X10 += X15;
                X5 = IntUtils.RotateLeft(X5 ^ X10, 12);
                X0 += X5;
                X15 = IntUtils.RotateLeft(X15 ^ X0, 8);
                X10 += X15;
                X5 = IntUtils.RotateLeft(X5 ^ X10, 7);
                X1 += X6;
                X12 = IntUtils.RotateLeft(X12 ^ X1, 16);
                X11 += X12;
                X6 = IntUtils.RotateLeft(X6 ^ X11, 12);
                X1 += X6;
                X12 = IntUtils.RotateLeft(X12 ^ X1, 8);
                X11 += X12;
                X6 = IntUtils.RotateLeft(X6 ^ X11, 7);
                X2 += X7;
                X13 = IntUtils.RotateLeft(X13 ^ X2, 16);
                X8 += X13;
                X7 = IntUtils.RotateLeft(X7 ^ X8, 12);
                X2 += X7;
                X13 = IntUtils.RotateLeft(X13 ^ X2, 8);
                X8 += X13;
                X7 = IntUtils.RotateLeft(X7 ^ X8, 7);
                X3 += X4;
                X14 = IntUtils.RotateLeft(X14 ^ X3, 16);
                X9 += X14;
                X4 = IntUtils.RotateLeft(X4 ^ X9, 12);
                X3 += X4;
                X14 = IntUtils.RotateLeft(X14 ^ X3, 8);
                X9 += X14;
                X4 = IntUtils.RotateLeft(X4 ^ X9, 7);
                ctr -= 2;
            }

            IntUtils.Le32ToBytes(X0 + m_wrkState[ctr], Output, OutOffset); OutOffset += 4;
            IntUtils.Le32ToBytes(X1 + m_wrkState[++ctr], Output, OutOffset); OutOffset += 4;
            IntUtils.Le32ToBytes(X2 + m_wrkState[++ctr], Output, OutOffset); OutOffset += 4;
            IntUtils.Le32ToBytes(X3 + m_wrkState[++ctr], Output, OutOffset); OutOffset += 4;
            IntUtils.Le32ToBytes(X4 + m_wrkState[++ctr], Output, OutOffset); OutOffset += 4;
            IntUtils.Le32ToBytes(X5 + m_wrkState[++ctr], Output, OutOffset); OutOffset += 4;
            IntUtils.Le32ToBytes(X6 + m_wrkState[++ctr], Output, OutOffset); OutOffset += 4;
            IntUtils.Le32ToBytes(X7 + m_wrkState[++ctr], Output, OutOffset); OutOffset += 4;
            IntUtils.Le32ToBytes(X8 + m_wrkState[++ctr], Output, OutOffset); OutOffset += 4;
            IntUtils.Le32ToBytes(X9 + m_wrkState[++ctr], Output, OutOffset); OutOffset += 4;
            IntUtils.Le32ToBytes(X10 + m_wrkState[++ctr], Output, OutOffset); OutOffset += 4;
            IntUtils.Le32ToBytes(X11 + m_wrkState[++ctr], Output, OutOffset); OutOffset += 4;
            IntUtils.Le32ToBytes(X12 + Counter[0], Output, OutOffset); OutOffset += 4;
            IntUtils.Le32ToBytes(X13 + Counter[1], Output, OutOffset); OutOffset += 4;
            IntUtils.Le32ToBytes(X14 + m_wrkState[++ctr], Output, OutOffset); OutOffset += 4;
            IntUtils.Le32ToBytes(X15 + m_wrkState[++ctr], Output, OutOffset);
        }
        #endregion

        #region Helpers
        private void Increment(uint[] Counter)
        {
            if (++Counter[0] == 0)
                ++Counter[1];
        }

        private uint[] Increase(uint[] Counter, int Size)
        {
            uint[] copy = new uint[Counter.Length];
            Array.Copy(Counter, 0, copy, 0, Counter.Length);

            for (int i = 0; i < Size; i++)
                Increment(copy);

            return copy;
        }

        void Scope()
        {
            m_processorCount = Environment.ProcessorCount;
            if (ProcessorCount % 2 != 0)
                ProcessorCount--;

            if (m_processorCount > 1)
            {
                if (m_parallelOption != null && m_parallelOption.MaxDegreeOfParallelism > 0 && (m_parallelOption.MaxDegreeOfParallelism % 2 == 0))
                    m_processorCount = m_parallelOption.MaxDegreeOfParallelism;
                else
                    m_parallelOption = new ParallelOptions() { MaxDegreeOfParallelism = m_processorCount };
            }

            m_parallelMinimumSize = m_processorCount * BLOCK_SIZE;
            m_parallelBlockSize = m_processorCount * PRL_BLOCKCACHE;

            if (!m_isInitialized)
                m_isParallel = (m_processorCount > 1);
        }

        private bool ValidCode(byte[] Code)
        {
            int ctr = 0;
            int rep = 0;

            // test for minimum asymmetry per sigma and tau constants; 
            // max 2 repeats, 2 times, distance of more than 4
            for (int i = 0; i < Code.Length; i++)
            {
                ctr = 0;
                for (int j = i + 1; j < Code.Length; j++)
                {
                    if (Code[i] == Code[j])
                    {
                        ctr++;

                        if (ctr > 1)
                            return false;
                        if (j - i < 5)
                            return false;
                    }
                }

                if (ctr == 1)
                    rep++;
                if (rep > 2)
                    return false;
            }

            return true;
        }
        #endregion

        #region IDispose
        /// <summary>
        /// Dispose of this class
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool Disposing)
        {
            if (!m_isDisposed && Disposing)
            {
                try
                {
                    if (m_wrkBuffer != null)
                    {
                        Array.Clear(m_wrkBuffer, 0, m_wrkBuffer.Length);
                        m_wrkBuffer = null;
                    }
                    if (m_wrkState != null)
                    {
                        Array.Clear(m_wrkState, 0, m_wrkState.Length);
                        m_wrkState = null;
                    }
                    if (m_dstCode != null)
                    {
                        Array.Clear(m_dstCode, 0, m_dstCode.Length);
                        m_dstCode = null;
                    }
                    m_isInitialized = false;
                    m_isParallel = false;
                    m_parallelBlockSize = 0;
                    m_rndCount = 0;
                }
                finally
                {
                    m_isDisposed = true;
                }
            }
        }
        #endregion
    }
}
