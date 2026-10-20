#ifndef CRUDEX_HARBOUR_PREPROCESSOR_H
#define CRUDEX_HARBOUR_PREPROCESSOR_H

#include <stddef.h>

#if defined(_WIN32)
#  define PP_API __declspec(dllexport)
#  define PP_CALL __cdecl
#else
#  define PP_API __attribute__((visibility("default")))
#  define PP_CALL
#endif

#ifdef __cplusplus
extern "C" {
#endif

/* ABI 1. Buffers are UTF-8, lengths are bytes, strings never include a BOM.
 * Return: 0 = success; 1 = preprocessor error; 2 = allocation failure;
 * 3 = invalid arguments. Free BOTH returned buffers with hbpp_free_buffer.
 * Rules and input are processed in a new context on every call.
 */
PP_API int PP_CALL hbpp_process(
    const unsigned char *source, size_t source_length,
    const unsigned char *rules, size_t rules_length,
    const char *include_path,
    char **output, size_t *output_length,
    char **diagnostics, size_t *diagnostics_length);

PP_API void PP_CALL hbpp_free_buffer(void *buffer);
PP_API const char *PP_CALL hbpp_version(void);

#ifdef __cplusplus
}
#endif
#endif
