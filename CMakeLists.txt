cmake_minimum_required(VERSION 3.20)
project(gepe3d LANGUAGES CXX C)
set(CMAKE_EXPORT_COMPILE_COMMANDS ON)
set(CMAKE_CXX_STANDARD 23)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)


# set some glfw flags to avoid building extra fluff
set(GLFW_BUILD_DOCS OFF CACHE BOOL "" FORCE)
set(GLFW_BUILD_TESTS OFF CACHE BOOL "" FORCE)
set(GLFW_BUILD_EXAMPLES OFF CACHE BOOL "" FORCE)

include(FetchContent)
FetchContent_Declare(
    glfw
    GIT_REPOSITORY https://github.com/glfw/glfw.git
    GIT_TAG        3.4
)
FetchContent_MakeAvailable(glfw)

set(RESOURCE_FILES kernels.cl point_sphere.vert point_sphere.frag)
find_program(XXD xxd REQUIRED)
message(STATUS "Found xxd, embedding resource files into header files")
foreach(RESOURCE_FILE ${RESOURCE_FILES})
    set(RESOURCE_PATH "${CMAKE_SOURCE_DIR}/res/${RESOURCE_FILE}")
    set(HEADER_PATH   "${CMAKE_BINARY_DIR}/${RESOURCE_FILE}.h")
    add_custom_command(
        OUTPUT ${HEADER_PATH}
        # xxd [options] [infile [outfile]]
        COMMAND ${XXD} -n ${RESOURCE_FILE} -i ${RESOURCE_PATH} ${HEADER_PATH}
        DEPENDS ${RESOURCE_PATH}
    )
    list(APPEND EMBED_HEADERS ${HEADER_PATH})
    message(STATUS "Generated embedded resource header ${HEADER_PATH}")
endforeach()

add_executable(${PROJECT_NAME}
    src/main.cpp
    src/particle_renderer.cpp
    glad/src/gl.c
    ${EMBED_HEADERS}
)
target_link_libraries(${PROJECT_NAME} glfw)
target_include_directories(${PROJECT_NAME} PRIVATE
    include
    glad/include
    ${CMAKE_BINARY_DIR}
)
target_compile_options(${PROJECT_NAME} PRIVATE
    -Wall
    -Wextra
    -Wpedantic
    -Werror
)
