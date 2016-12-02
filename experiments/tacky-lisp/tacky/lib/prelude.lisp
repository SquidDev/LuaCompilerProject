;; Creates a top-level function
(define-macro defun (lambda (name args &body)
  `(define ,name (lambda ,args ,@body))))

;; Create a top-level macro
(define-macro defmacro (lambda (name args &body)
  `(define-macro ,name (lambda ,args ,@body))))

;; Creates a code block, for use where an expression would normally be required.
(defmacro progn (&body)
  `((lambda () ,@body)))

;; Evaluates a condition, evaluating the second argument if truthy, the third
;; argument if falsey.
(defmacro if (c t b) `(cond (,c ,t) (true ,b)))

;; Create a random symbol
(define-native gensym)

;; Perform an action whilst a value is true
(defmacro while (check &body)
  (let* ((impl (gensym)))
    `(progn
      (letrec ((,impl (lambda ()
                        (cond
                          (,check ,@body (,impl))
                          (true)))))
        (,impl)))))

(defmacro for (ctr start end step &body)
  (let* ((impl (gensym))
         (ctr' (gensym))
         (end' (gensym))
         (step' (gensym)))
    `(let* ((,end' ,end)
              (,step' ,step)
              (,impl nil))
       (set! ,impl (lambda (,ctr')
                     (cond
                       ((if (< 0 ,step) (<= ,ctr' ,end') (>= ,ctr' ,end'))
                         (let* ((,ctr ,ctr')) ,@body)
                         (,impl (+ ,ctr' ,step')))
                       (true))))
       (,impl ,start))))

(defmacro for-each (var lst &body)
  (let ((ctr' (gensym))
        (lst' (gensym)))
       `(with (,lst' ,lst)
         (for ,ctr' 1 (# ,lst') 1 (with (,var (get-idx ,lst' ,ctr')) ,@body)))))

(defmacro and (a b)
  (with (symb (gensym))
    `(with (,symb ,a) (if ,symb ,b ,symb))))

(defmacro or (a b)
  (with (symb (gensym))
    `(with (,symb ,a) (if ,symb ,symb ,b))))

(defun ! (expr) (cond (expr false) (true true)))

;; Print one or more values to standard output
(define-native print!)

;; Pretty-print one or more values to standard ouput
(define-native pretty-print!)

(define-native dump-node!)
(defun debug! (x)
  (dump-node! x)
  x)

;; Get a table key
(define-native get-idx)

;; Set a table key to a value
(define-native set-idx!)

(define-native ==)
(define-native ~=)
(define-native <)
(define-native <=)
(define-native >)
(define-native >=)

(define-native +)
(define-native -)
(define-native *)
(define-native /)
(define-native %)
(define-native ^)

(define-native invoke-dynamic)
(define-native type)
(define-native struct)

(define-native error)
(define-native assert)

(defun list? (x) (== (type x) "list"))
(defun string? (x) (== (type x) "string"))
(defun number? (x) (== (type x) "number"))
(defun symbol? (x) (== (type x) "symbol"))
(defun boolean? (x) (== (type x) "boolean"))

;; Check if this is a list and it is empty
(defun nil? (x) (if (list? x) (== (# x) 0) false))

(defun symbol->string (x) (if (symbol? x) (get-idx x "contents") nil))
(defun number->string (x) (if (symbol? x) (get-idx x "contents") nil))

;; TODO: Fix up the resolver
(defun /= (x y) (~= x y))
(defun = (x y) (== x y))

;; Get the length of a list
(defun # (li) (get-idx li "n"))

;; Push an entry on to the end of this list
(defun push-cdr! (li val)
  (let* ((len (+ (# li) 1)))
    (set-idx! li "n" len)
    (set-idx! li len val)
    li))

;; Build a list from the arguments
(defun list (&entries) entries)

;; Map a function over every item in the list, creating a new list
(defun map (fn li)
  (let* ((out '()))
    (set-idx! out "n" (# li))
    (for i 1 (# li) 1 (set-idx! out i (fn (get-idx li i))))
    out))

(defun iter (fn li)
  (for i 1 (# li) 1 (fn (get-idx li i))))

(defun traverse (li fn) (map fn li))

(defun car (xs) (get-idx xs 1))
(define-native cdr)

(defun cars (xs) (map car xs))
(defun cdrs (xs) (map cdr xs))

(defun caar (x) (car (car x)))
(defun cadr (x) (car (cdr x)))
(defun cdar (x) (cdr (car x)))
(defun cddr (x) (cdr (cdr x)))
(defun caaar (x) (car (car (car x))))
(defun caadr (x) (car (car (cdr x))))
(defun cadar (x) (car (cdr (car x))))
(defun caddr (x) (car (cdr (cdr x))))
(defun cdaar (x) (cdr (car (car x))))
(defun cdadr (x) (cdr (car (cdr x))))
(defun cddar (x) (cdr (cdr (car x))))
(defun cdddr (x) (cdr (cdr (cdr x))))
(defun caaaar (x) (car (car (car (car x)))))
(defun caaadr (x) (car (car (car (cdr x)))))
(defun caadar (x) (car (car (cdr (car x)))))
(defun caaddr (x) (car (car (cdr (cdr x)))))
(defun cadaar (x) (car (cdr (car (car x)))))
(defun cadadr (x) (car (cdr (car (cdr x)))))
(defun caddar (x) (car (cdr (cdr (car x)))))
(defun cadddr (x) (car (cdr (cdr (cdr x)))))
(defun cdaaar (x) (cdr (car (car (car x)))))
(defun cdaadr (x) (cdr (car (car (cdr x)))))
(defun cdadar (x) (cdr (car (cdr (car x)))))
(defun cdaddr (x) (cdr (car (cdr (cdr x)))))
(defun cddaar (x) (cdr (cdr (car (car x)))))
(defun cddadr (x) (cdr (cdr (car (cdr x)))))
(defun cdddar (x) (cdr (cdr (cdr (car x)))))
(defun cddddr (x) (cdr (cdr (cdr (cdr x)))))

(defun caars (xs) (map caar xs))
(defun cadrs (xs) (map cadr xs))
(defun cdars (xs) (map cdar xs))
(defun cddrs (xs) (map cddr xs))
(defun caaars (xs) (map caaar xs))
(defun caadrs (xs) (map caadr xs))
(defun cadars (xs) (map cadar xs))
(defun caddrs (xs) (map caddr xs))
(defun cdaars (xs) (map cdaar xs))
(defun cdadrs (xs) (map cdadr xs))
(defun cddars (xs) (map cddar xs))
(defun cdddrs (xs) (map cdddr xs))
(defun caaaars (xs) (map caaaar xs))
(defun caaadrs (xs) (map caaadr xs))
(defun caadars (xs) (map caadar xs))
(defun caaddrs (xs) (map caaddr xs))
(defun cadaars (xs) (map cadaar xs))
(defun cadadrs (xs) (map cadadr xs))
(defun caddars (xs) (map caddar xs))
(defun cadddrs (xs) (map cadddr xs))
(defun cdaaars (xs) (map cdaaar xs))
(defun cdaadrs (xs) (map cdaadr xs))
(defun cdadars (xs) (map cdadar xs))
(defun cdaddrs (xs) (map cdaddr xs))
(defun cddaars (xs) (map cddaar xs))
(defun cddadrs (xs) (map cddadr xs))
(defun cdddars (xs) (map cdddar xs))
(defun cddddrs (xs) (map cddddr xs))

;; Binds a variable to an expression
(defmacro let (vars &body)
  `((lambda ,(cars vars) ,@body) ,@(cadrs vars)))

(defmacro let* (vars &body)
  ; Note, this depends on as few library functions as possible: it is used
  ; by most macros to "bootstrap" then language.
  (if (! (nil? vars))
    `((lambda (,(caar vars)) (let* ,(cdr vars) ,@body)) ,(cadar vars))
    `((lambda () ,@body))))

(defmacro letrec (vars &body)
  `((lambda ,(cars vars)
    ,@(map (lambda (var) `(set! ,(car var) ,(cadr var))) vars)
    ,@body)))

;; Binds a single variable
(defmacro with (var &body) `((lambda (,(car var)) ,@body) ,(cadr var)))

;; Return a new list where only the predicate matches
(defun filter (fn li)
  (with (out '())
    (for i 1 (# li 1) 1 (let ((item (get-idx li i)))
      (if (fn item) (push-cdr! out item))))
    out))

;; Determine whether any element matches a predicate
(defun any (fn li)
  (letrec ((any-impl (lambda (i)
                       (cond
                         ((> i (# li)) false)
                         ((fn (get-idx li i)) true)
                         (true (any-impl (+ i 1)))))))
    (any-impl 1)))

;; Determine whether all elements match a predicate
(defun all (fn li)
  (letrec ((all-impl (lambda(i)
                       (cond
                         ((> i (# li)) true)
                         ((fn (get-idx li i)) (all-impl (+ i 1)))
                         (true false)))))
    (all-impl 1)))

;; Fold left across a list
(defun foldl (func accum li)
  (for i 1 (# li) 1
    (set! accum (func accum (get-idx li i))))
  accum)

;; Fold right across a list
(defun foldr (func accum li)
  (for i (# li) 1 -1
    (set! accum (func (get-idx li i) accum)))
  accum)

(defmacro case (x &cases)
  (let* ((name (gensym))
         (transform-case (lambda (case)
                           (if (list? case)
                             `((,@(car case) ,name) ,@(cdr case))
                             `(true)))))
    `((lambda (,name) (cond ,@(map transform-case cases))) ,x)))

(defun succ (x) (+ 1 x))
(defun pred (x) (- x 1))

;; Checks if this symbol is a wildcard
(defun is-slot (symb) (= (symbol->string symb) "<>"))

;; Partially apply a function, where <> is replaced by an argument to a function.
;; Values are evaluated every time the resulting function is called.
(defmacro cut (&func)
  (let ((args '())
        (call '()))
    (for-each item func
      (if (is-slot item)
        (with (symb (gensym))
          (push-cdr! args symb)
          (push-cdr! call symb))
        (push-cdr! call item)))
    `(lambda ,args ,call)))

;; Partially apply a function, where <> is replaced by an argument to a function.
;; Values are evaluated when this function is defined.
(defmacro cute (&func)
  (let ((args '())
        (vals '())
        (call '()))
    (for-each item func
      (with (symb (gensym))
        (push-cdr! call symb)
        (if (is-slot item)
          (push-cdr! args symb)
          (push-cdr! vals (list symb item)))))
    `(let ,vals (lambda ,args ,call))))

;; Chain a series of method calls together.
;; If the list contains <> then the value is placed there, otherwise the expression is invoked
;; with the previous entry as an argument
(defmacro -> (x &funcs)
  (with (res x)
    (for-each form funcs
      (if (and (list? form) (any is-slot form))
        (set! res (map (lambda (x) (if (is-slot x) res x)) form))
        (set! res (list form res))))
    res))

;; Chain a series of index accesses together
(defmacro .> (x &keys)
  (with (res x)
    (for-each key keys (set! res `(get-idx ,res ,key)))
    res))
