using Microsoft.Research.APSI.Server;
using System.IO;
using Xunit;

namespace APSILibraryTests
{
    public class JSONParametersTest
    {
        [Fact]
        public void ParseTest()
        {
            string json1 = @"
            {
                ""table_params"": {
                    ""hash_func_count"": 1,
                    ""table_size"": 256,
                    ""max_items_per_bin"": 35
                },
                ""item_params"": {
                    ""felts_per_item"": 8
                },
                ""query_params"": {
                    ""ps_low_degree"": 0,
                    ""query_powers"": [ 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35 ]
                },
                ""seal_params"": {
                    ""plain_modulus"": 12289,
                    ""poly_modulus_degree"": 2048,
                    ""coeff_modulus_bits"": [ 40 ]
                }
            }";

            string json2 = @"
            {
                ""table_params"": {
                    ""hash_func_count"": 3,
                    ""table_size"": 512,
                    ""max_items_per_bin"": 92
                },
                ""item_params"": {
                    ""felts_per_item"": 8
                },
                ""query_params"": {
                    ""ps_low_degree"": 0,
                    ""query_powers"": [ 1, 3, 4, 5, 8, 14, 20, 26, 32, 38, 41, 42, 43, 45, 46 ]
                },
                ""seal_params"": {
                    ""plain_modulus"": 40961,
                    ""poly_modulus_degree"": 4096,
                    ""coeff_modulus_bits"": [ 40, 32, 32 ]
                }
            }";

            string json3 = @"
            {
                ""table_params"": {
                    ""hash_func_count"": 1,
                                ""table_size"": 256,
                                ""max_items_per_bin"": 72
                            },
                            ""item_params"": {
                    ""felts_per_item"": 8
                            },
                            ""query_params"": {
                    ""ps_low_degree"": 0,
                                ""query_powers"": [ 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72 ]
                            },
                            ""seal_params"": {
                    ""plain_modulus"": 12289,
                                ""poly_modulus_degree"": 2048,
                                ""coeff_modulus_bits"": [ 40 ]
                            }
            }";

            string json4 = @"
            {
                ""table_params"": {
                    ""hash_func_count"": 3,
                                ""table_size"": 512,
                                ""max_items_per_bin"": 180
                            },
                            ""item_params"": {
                    ""felts_per_item"": 8
                            },
                            ""query_params"": {
                    ""ps_low_degree"": 0,
                                ""query_powers"": [ 1, 3, 4, 6, 10, 13, 15, 21, 29, 37, 45, 53, 61, 69, 77, 81, 83, 86, 87, 90, 92, 96 ]
                            },
                            ""seal_params"": {
                    ""plain_modulus"": 40961,
                                ""poly_modulus_degree"": 4096,
                                ""coeff_modulus_bits"": [ 40, 32, 32 ]
                            }
            }";

            APSIParams parms1 = new(json1);
            Assert.NotNull(parms1);

            APSIParams params2 = new(json2);
            Assert.NotNull(params2);

            APSIParams params3 = new(json3);
            Assert.NotNull(params3);

            APSIParams params4 = new(json4);
            Assert.NotNull(params4);
        }

        [Fact]
        public void FileParamsTest()
        {
            string json = @"
            {
                ""table_params"": {
                    ""hash_func_count"": 3,
                                ""table_size"": 512,
                                ""max_items_per_bin"": 180
                            },
                            ""item_params"": {
                    ""felts_per_item"": 8
                            },
                            ""query_params"": {
                    ""ps_low_degree"": 0,
                                ""query_powers"": [ 1, 3, 4, 6, 10, 13, 15, 21, 29, 37, 45, 53, 61, 69, 77, 81, 83, 86, 87, 90, 92, 96 ]
                            },
                            ""seal_params"": {
                    ""plain_modulus"": 40961,
                                ""poly_modulus_degree"": 4096,
                                ""coeff_modulus_bits"": [ 40, 32, 32 ]
                            }
            }";

            string tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, json);

            APSIParams parameters = new(tempFile);
            Assert.NotNull(parameters);

            File.Delete(tempFile);
        }
    }
}
