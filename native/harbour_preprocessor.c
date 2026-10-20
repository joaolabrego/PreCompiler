#include "harbour_preprocessor.h"
#include "hbapi.h"
#include "hbpp.h"
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#if defined(_WIN32)
#include <windows.h>
static SRWLOCK process_lock = SRWLOCK_INIT;
#define ENTER() AcquireSRWLockExclusive(&process_lock)
#define LEAVE() ReleaseSRWLockExclusive(&process_lock)
#else
#include <pthread.h>
static pthread_mutex_t process_lock = PTHREAD_MUTEX_INITIALIZER;
#define ENTER() pthread_mutex_lock(&process_lock)
#define LEAVE() pthread_mutex_unlock(&process_lock)
#endif

typedef struct {
    char *data;
    size_t length;
    size_t capacity;
    int failed;
} Buffer;

typedef struct {
    PHB_PP_STATE state;
    Buffer diagnostics;
} Context;

static int append(Buffer *buffer, const char *text, size_t length)
{
    size_t required;
    size_t capacity;
    char *data;
    if (buffer->failed)
        return 0;
    if (length > SIZE_MAX - buffer->length - 1) {
        buffer->failed = 1;
        return 0;
    }
    required = buffer->length + length + 1;
    if (required > buffer->capacity) {
        capacity = buffer->capacity ? buffer->capacity : 256;
        while (capacity < required) {
            if (capacity > SIZE_MAX / 2) {
                capacity = required;
                break;
            }
            capacity *= 2;
        }
        data = (char *) realloc(buffer->data, capacity);
        if (!data) {
            buffer->failed = 1;
            return 0;
        }
        buffer->data = data;
        buffer->capacity = capacity;
    }
    if (length)
        memcpy(buffer->data + buffer->length, text, length);
    buffer->length += length;
    buffer->data[buffer->length] = '\0';
    return 1;
}

static void report_error(void *cargo, const char * const *messages, char type,
                         int number, const char *first, const char *second)
{
    Context *context = (Context *) cargo;
    char prefix[128];
    char message[4096];
    const char *file = hb_pp_fileName(context->state);
    int count = snprintf(prefix, sizeof(prefix), "%s %d, line %d: ",
                         type == 'W' ? "Warning" : "Error", number,
                         hb_pp_line(context->state));
    if (count > 0)
        append(&context->diagnostics, prefix,
               (size_t) count < sizeof(prefix) ? (size_t) count : sizeof(prefix) - 1);
    if (file) {
        append(&context->diagnostics, file, strlen(file));
        append(&context->diagnostics, ": ", 2);
    }
    count = snprintf(message, sizeof(message), messages[number - 1],
                     first ? first : "", second ? second : "");
    if (count > 0)
        append(&context->diagnostics, message,
               (size_t) count < sizeof(message) ? (size_t) count : sizeof(message) - 1);
    append(&context->diagnostics, "\n", 1);
}

static void report_text(void *cargo, const char *text)
{
    Context *context = (Context *) cargo;
    append(&context->diagnostics, text, strlen(text));
}

/* hbcommon's version helpers expect this symbol from the embedding program. */
HB_MAXINT hb_verRevision(void)
{
    return 2610171956LL;
}

PP_API const char *PP_CALL hbpp_version(void)
{
    return "Crudex Harbour Preprocessor ABI 1; Harbour f3d0db2c24ce36d1869a41176085b4a57cade340";
}

PP_API void PP_CALL hbpp_free_buffer(void *buffer)
{
    free(buffer);
}

PP_API int PP_CALL hbpp_process(
    const unsigned char *source, size_t source_length,
    const unsigned char *rules, size_t rules_length,
    const char *include_path,
    char **output, size_t *output_length,
    char **diagnostics, size_t *diagnostics_length)
{
    Context context = {0};
    Buffer result = {0};
    HB_SIZE length;
    char *line;
    int status = 0;

    if (!output || !output_length || !diagnostics || !diagnostics_length)
        return 3;
    *output = NULL;
    *output_length = 0;
    *diagnostics = NULL;
    *diagnostics_length = 0;
    if ((!source && source_length) || (!rules && rules_length))
        return 3;

    ENTER();
    context.state = hb_pp_new();
    hb_pp_init(context.state, HB_TRUE, HB_FALSE, 0, &context,
               NULL, NULL, report_error, report_text, NULL, NULL, NULL);

    if (include_path && *include_path)
        hb_pp_addSearchPath(context.state, include_path, HB_FALSE);

    /* Deliberately no std.ch: the caller supplies the DSL's rules. */
    hb_pp_inBuffer(context.state, "rules.ch",
                   rules ? (const char *) rules : "", (HB_SIZE) rules_length, 0);
    while ((line = hb_pp_nextLine(context.state, &length)) != NULL && length != 0) {
        if (context.diagnostics.failed)
            break;
    }
    if (hb_pp_errorCount(context.state) != 0)
        status = 1;

    if (!status && !context.diagnostics.failed) {
        hb_pp_inBuffer(context.state, "source.prg",
                       source ? (const char *) source : "", (HB_SIZE) source_length, 0);
        while ((line = hb_pp_nextLine(context.state, &length)) != NULL && length != 0) {
            if (!append(&result, line, (size_t) length) || context.diagnostics.failed)
                break;
        }
        if (hb_pp_errorCount(context.state) != 0)
            status = 1;
    }

    if (!result.data)
        append(&result, "", 0);
    if (!context.diagnostics.data)
        append(&context.diagnostics, "", 0);
    if (result.failed || context.diagnostics.failed)
        status = 2;

    hb_pp_free(context.state);
    LEAVE();

    *output = result.data;
    *output_length = result.length;
    *diagnostics = context.diagnostics.data;
    *diagnostics_length = context.diagnostics.length;
    return status;
}
