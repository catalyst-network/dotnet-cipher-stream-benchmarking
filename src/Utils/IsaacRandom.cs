﻿namespace benchmark.utils
{
    /// <summary>
    /// The Isaac Random
    /// </summary>
    public class IsaacRandom
    {
        // external results 
        readonly uint[] _randRsl = new uint[256];
        uint _randCnt;

        // internal state 
        readonly uint[] _mm = new uint[256];
        uint _aa, _bb, _cc;

        void Isaac()
        {
            uint i;
            _cc++; // _cc just gets incremented once per 256 results 
            _bb += _cc; // then combined with _bb 

            for (i = 0; i <= 255; i++)
            {
                var x = _mm[i];
                switch (i & 3)
                {
                    case 0:
                        _aa = _aa ^ (_aa << 13);
                        break;
                    case 1:
                        _aa = _aa ^ (_aa >> 6);
                        break;
                    case 2:
                        _aa = _aa ^ (_aa << 2);
                        break;
                    case 3:
                        _aa = _aa ^ (_aa >> 16);
                        break;
                }

                _aa = _mm[(i + 128) & 255] + _aa;
                var y = _mm[(x >> 2) & 255] + _aa + _bb;
                _mm[i] = y;
                _bb = _mm[(y >> 10) & 255] + x;
                _randRsl[i] = _bb;
            }
        }

        void Mix(ref uint a, ref uint b, ref uint c, ref uint d, ref uint e, ref uint f, ref uint g, ref uint h)
        {
            a = a ^ b << 11;
            d += a;
            b += c;
            b = b ^ c >> 2;
            e += b;
            c += d;
            c = c ^ d << 8;
            f += c;
            d += e;
            d = d ^ e >> 16;
            g += d;
            e += f;
            e = e ^ f << 10;
            h += e;
            f += g;
            f = f ^ g >> 4;
            a += f;
            g += h;
            g = g ^ h << 8;
            b += g;
            h += a;
            h = h ^ a >> 9;
            c += h;
            a += b;
        }

        /// <summary>Initializes the instance.</summary>
        /// <param name="flag">if set to <c>true</c> [flag]
        /// use the contents of _randRsl[] to initialize _mm[].
        /// </param>
        void Init(bool flag)
        {
            short i;

            _aa = 0;
            _bb = 0;
            _cc = 0;
            var a = 0x9e3779b9;
            var b = a;
            var c = a;
            var d = a;
            var e = a;
            var f = a;
            var g = a;
            var h = a;

            for (i = 0; i <= 3; i++) // scramble it 
                Mix(ref a, ref b, ref c, ref d, ref e, ref f, ref g, ref h);

            i = 0;
            do
            {
                // fill in _mm[] with messy stuff  
                if (flag)
                {
                    // use all the information in the seed 
                    a += _randRsl[i];
                    b += _randRsl[i + 1];
                    c += _randRsl[i + 2];
                    d += _randRsl[i + 3];
                    e += _randRsl[i + 4];
                    f += _randRsl[i + 5];
                    g += _randRsl[i + 6];
                    h += _randRsl[i + 7];
                } // if flag

                Mix(ref a, ref b, ref c, ref d, ref e, ref f, ref g, ref h);
                _mm[i] = a;
                _mm[i + 1] = b;
                _mm[i + 2] = c;
                _mm[i + 3] = d;
                _mm[i + 4] = e;
                _mm[i + 5] = f;
                _mm[i + 6] = g;
                _mm[i + 7] = h;
                i += 8;
            } while (i < 255);

            if (flag)
            {
                // do a second pass to make all of the seed affect all of _mm 
                i = 0;
                do
                {
                    a += _mm[i];
                    b += _mm[i + 1];
                    c += _mm[i + 2];
                    d += _mm[i + 3];
                    e += _mm[i + 4];
                    f += _mm[i + 5];
                    g += _mm[i + 6];
                    h += _mm[i + 7];
                    Mix(ref a, ref b, ref c, ref d, ref e, ref f, ref g, ref h);
                    _mm[i] = a;
                    _mm[i + 1] = b;
                    _mm[i + 2] = c;
                    _mm[i + 3] = d;
                    _mm[i + 4] = e;
                    _mm[i + 5] = f;
                    _mm[i + 6] = g;
                    _mm[i + 7] = h;
                    i += 8;
                } while (i < 255);
            }

            Isaac(); // fill in the first set of results 
            _randCnt = 0; // prepare to use the first set of results 
        }

        /// <summary>Initializes a new instance of the <see cref="IsaacRandom"/> class.</summary>
        /// <param name="seed">The seed.</param>
        public IsaacRandom(string seed)
        {
            for (int i = 0; i < 256; i++) _mm[i] = 0;
            for (int i = 0; i < 256; i++) _randRsl[i] = 0;
            int m = seed.Length;
            for (int i = 0; i < m; i++)
            {
                _randRsl[i] = seed[i];
            }

            // initialize ISAAC with seed
            Init(true);
        }

        /// <summary>Gets the next random 32 bit value.</summary>
        /// <returns></returns>
        public uint NextInt()
        {
            uint result = _randRsl[_randCnt];
            _randCnt++;
            if (_randCnt > 255)
            {
                Isaac();
                _randCnt = 0;
            }

            return result;
        }

        /// <summary>Gets random char byte.</summary>
        /// <returns>ASCII byte</returns>
        public byte NextByte()
        {
            return (byte) (NextInt() % 95 + 32);
        }
    }

}
