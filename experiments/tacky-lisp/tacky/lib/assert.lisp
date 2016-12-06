(import string)

(define-native assert!)

(defmacro assert (&assertions)
  (let ((handle-eq
          (lambda (tree)
            `(assert/assert!
               (= ,(car tree) ,(cadr tree))
               ,(string/..
                  "expected "
                  (string/->string (car tree))
                  " and "
                  (string/->string (cadr tree))
                  " to be equal"))))
        )
    (traverse assertions
              (lambda (x)
                (case (symbol->string (car x))
                  ((= "=") (handle-eq (cdr x))))))))

