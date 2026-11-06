grammar:
    line=varSet|fnCall
    varSet=$IDENT=expr
    fnCall=$IDENT(expr[argEps])
    argEps=,expr[argEps]|ε
    IDENT=[LETTER]?[LETTER|DIGIT]*
    expr=NUMBER|STRING
    NUMBER=[DIGIT]+
    STRING='[any]'|"[any]"    


grammar:
    comment=#[any]*
    line=varSet|fnCall
    varSet=$IDENT=expr
    fnCall=$IDENT(expr[argEps])
    argEps=,expr[argEps]|ε
    IDENT=[LETTER]?[LETTER|DIGIT]*
    expr=number|string
    number=[DIGIT]+
    string='[any]'|"[any]"   

    todo: concat strings like a+b, a+(c+d)
    or interpolate 'my $(VAR)'
 