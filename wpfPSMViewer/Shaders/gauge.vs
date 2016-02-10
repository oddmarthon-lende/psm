/*
    Copyright © 2014 Odd Marthon Lende
    All Rights Reserved
*/

attribute vec3 position;

varying float angle;

void main () {

    angle = position.z;
    gl_Position = vec4(position.xy, 1., 1.);

}